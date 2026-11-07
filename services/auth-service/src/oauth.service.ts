import { Injectable, Logger } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { createHash, createPublicKey, randomBytes, timingSafeEqual, verify as cryptoVerify } from 'crypto';
import { cfg } from './config';

export type Proveedor = 'google' | 'github';
export const PROVEEDORES: Proveedor[] = ['google', 'github'];

/** Datos de identidad ya verificados por el proveedor. `subject` es el identificador estable (nunca el correo). */
export interface PerfilExterno {
  proveedor: Proveedor;
  subject: string;
  email?: string;
  emailVerificado: boolean;
  nombre?: string;
}

/** Error de negocio del flujo externo: `codigo` es lo único que viaja al navegador (nunca texto libre del proveedor). */
export class OAuthError extends Error {
  constructor(public codigo: string, detalle?: string) { super(detalle ?? codigo); }
}

const TX_AUDIENCE = 'alerta.oauth.tx'; // audiencia propia: esta cookie jamás sirve como access token
const COOKIE = 'oauth_tx';
const COOKIE_PATH = '/api/auth/oauth';
const TIMEOUT_MS = 8000;

const b64u = (b: Buffer) => b.toString('base64url');
const seguro = (a: string, b: string) => a.length === b.length && timingSafeEqual(Buffer.from(a), Buffer.from(b));

/**
 * OAuth 2.0 Authorization Code + PKCE (RFC 6749 / 7636) y OpenID Connect (Google).
 *  - Google: OIDC. El `id_token` se valida (firma RS256 con JWKS, iss, aud, exp, nonce) antes de confiar en él.
 *  - GitHub: OAuth 2.0 puro. La identidad se obtiene con el access token (/user y /user/emails).
 * Sin base de datos: el contexto del intento (state, nonce, code_verifier) viaja en una cookie HttpOnly firmada,
 * por lo que funciona con varias réplicas del servicio.
 */
@Injectable()
export class OAuthService {
  private log = new Logger('OAuth');
  private discovery?: { at: number; doc: any };
  private jwks?: { at: number; keys: any[] };

  constructor(private jwt: JwtService) {}

  habilitado(p: string): p is Proveedor {
    return (PROVEEDORES as string[]).includes(p) && !!cfg.oauth[p as Proveedor].clientId && !!cfg.oauth[p as Proveedor].clientSecret;
  }
  proveedoresHabilitados(): Proveedor[] { return PROVEEDORES.filter((p) => this.habilitado(p)); }
  redirectUri(p: Proveedor) { return `${cfg.oauth.publicUrl}/api/auth/oauth/${p}/callback`; }
  private get secure() { return cfg.oauth.publicUrl.startsWith('https://'); }

  // ------------------------------------------------------------------ paso 1: redirigir al proveedor
  async iniciar(p: Proveedor): Promise<{ url: string; cookie: string }> {
    const state = b64u(randomBytes(24));
    const nonce = b64u(randomBytes(24));
    const verifier = b64u(randomBytes(48));                       // PKCE: 64 caracteres
    const challenge = b64u(createHash('sha256').update(verifier).digest());
    const base = { response_type: 'code', client_id: cfg.oauth[p].clientId, redirect_uri: this.redirectUri(p), state, code_challenge: challenge, code_challenge_method: 'S256' };

    let url: string;
    if (p === 'google') {
      const { authorization_endpoint } = await this.googleDoc();
      url = authorization_endpoint + '?' + new URLSearchParams({ ...base, scope: 'openid email profile', nonce, prompt: 'select_account' });
    } else {
      url = cfg.oauth.github.authorizeUrl + '?' + new URLSearchParams({ ...base, scope: 'read:user user:email', allow_signup: 'true' });
    }
    const tx = this.jwt.sign({ p, st: state, nonce, ver: verifier }, { secret: cfg.jwtKey, issuer: cfg.issuer, audience: TX_AUDIENCE, expiresIn: cfg.oauth.stateMinutes * 60 });
    return { url, cookie: this.setCookie(tx, cfg.oauth.stateMinutes * 60) };
  }

  // ------------------------------------------------------------------ paso 2: callback del proveedor
  async completar(p: Proveedor, query: { code?: string; state?: string; error?: string }, cookieHeader?: string): Promise<PerfilExterno> {
    if (query.error) throw new OAuthError('acceso_denegado', `El proveedor devolvió error=${String(query.error).slice(0, 60)}`);
    const raw = this.leerCookie(cookieHeader);
    if (!raw || !query.state || !query.code) throw new OAuthError('estado_invalido', 'Falta cookie, state o code.');
    let tx: any;
    try { tx = this.jwt.verify(raw, { secret: cfg.jwtKey, issuer: cfg.issuer, audience: TX_AUDIENCE }); }
    catch { throw new OAuthError('estado_invalido', 'Cookie de estado inválida o expirada.'); }
    // Protección CSRF (RFC 6749 §10.12): el state de la URL debe ser el que este navegador recibió al iniciar.
    if (tx.p !== p || typeof query.state !== 'string' || !seguro(tx.st, query.state)) throw new OAuthError('estado_invalido', 'El state no coincide.');

    try {
      return p === 'google' ? await this.completarGoogle(String(query.code), tx) : await this.completarGithub(String(query.code), tx);
    } catch (e) {
      if (e instanceof OAuthError) throw e;
      this.log.warn(`Fallo en el intercambio con ${p}: ${(e as Error).message}`);
      throw new OAuthError('proveedor_error', (e as Error).message);
    }
  }

  // ------------------------------------------------------------------ Google (OpenID Connect)
  private async googleDoc() {
    if (this.discovery && Date.now() - this.discovery.at < 3_600_000) return this.discovery.doc;
    const doc = await this.json(cfg.oauth.google.discoveryUrl);
    if (!doc?.authorization_endpoint || !doc?.token_endpoint || !doc?.jwks_uri) throw new Error('Documento de descubrimiento OIDC incompleto.');
    this.discovery = { at: Date.now(), doc };
    return doc;
  }

  private async completarGoogle(code: string, tx: any): Promise<PerfilExterno> {
    const doc = await this.googleDoc();
    const tok = await this.json(doc.token_endpoint, {
      method: 'POST',
      headers: { 'content-type': 'application/x-www-form-urlencoded', accept: 'application/json' },
      body: new URLSearchParams({
        grant_type: 'authorization_code', code, redirect_uri: this.redirectUri('google'),
        client_id: cfg.oauth.google.clientId, client_secret: cfg.oauth.google.clientSecret, code_verifier: tx.ver,
      }).toString(),
    });
    if (!tok?.id_token) throw new Error('Google no devolvió id_token.');
    const c = await this.validarIdToken(String(tok.id_token), doc, tx.nonce);
    return { proveedor: 'google', subject: String(c.sub), email: c.email, emailVerificado: c.email_verified === true || c.email_verified === 'true', nombre: c.name };
  }

  /** Valida el id_token como exige OIDC Core §3.1.3.7: firma (JWKS), iss, aud, exp y nonce. */
  async validarIdToken(idToken: string, doc: any, nonce: string): Promise<any> {
    const partes = idToken.split('.');
    if (partes.length !== 3) throw new OAuthError('proveedor_error', 'id_token mal formado.');
    const header = JSON.parse(Buffer.from(partes[0], 'base64url').toString());
    const c = JSON.parse(Buffer.from(partes[1], 'base64url').toString());
    if (header.alg !== 'RS256') throw new OAuthError('proveedor_error', `Algoritmo no permitido: ${header.alg}`); // evita alg=none / confusión HS256

    const jwk = (await this.jwksKeys(doc.jwks_uri, header.kid)).find((k: any) => k.kid === header.kid);
    if (!jwk) throw new OAuthError('proveedor_error', 'Clave de firma desconocida (kid).');
    const ok = cryptoVerify('RSA-SHA256', Buffer.from(partes[0] + '.' + partes[1]), createPublicKey({ key: jwk, format: 'jwk' }), Buffer.from(partes[2], 'base64url'));
    if (!ok) throw new OAuthError('proveedor_error', 'Firma del id_token inválida.');

    const emisores = [doc.issuer, 'https://accounts.google.com', 'accounts.google.com'].filter(Boolean);
    const aud = Array.isArray(c.aud) ? c.aud : [c.aud];
    const ahora = Math.floor(Date.now() / 1000);
    if (!emisores.includes(c.iss)) throw new OAuthError('proveedor_error', 'Emisor (iss) no válido.');
    if (!aud.includes(cfg.oauth.google.clientId)) throw new OAuthError('proveedor_error', 'Audiencia (aud) no válida.');
    if (typeof c.exp !== 'number' || c.exp + 60 < ahora) throw new OAuthError('proveedor_error', 'id_token expirado.');
    if (!c.nonce || !seguro(String(c.nonce), nonce)) throw new OAuthError('proveedor_error', 'nonce no coincide (posible repetición).');
    if (!c.sub) throw new OAuthError('proveedor_error', 'id_token sin subject.');
    return c;
  }

  private async jwksKeys(uri: string, kid: string): Promise<any[]> {
    const vigente = this.jwks && Date.now() - this.jwks.at < 3_600_000;
    if (vigente && this.jwks!.keys.some((k) => k.kid === kid)) return this.jwks!.keys;
    const j = await this.json(uri); // si el kid no está (rotación de claves) se vuelve a pedir el JWKS
    this.jwks = { at: Date.now(), keys: j?.keys ?? [] };
    return this.jwks.keys;
  }

  // ------------------------------------------------------------------ GitHub (OAuth 2.0)
  private async completarGithub(code: string, tx: any): Promise<PerfilExterno> {
    const g = cfg.oauth.github;
    const tok = await this.json(g.tokenUrl, {
      method: 'POST',
      headers: { 'content-type': 'application/x-www-form-urlencoded', accept: 'application/json' },
      body: new URLSearchParams({ client_id: g.clientId, client_secret: g.clientSecret, code, redirect_uri: this.redirectUri('github'), code_verifier: tx.ver }).toString(),
    });
    if (!tok?.access_token) throw new OAuthError('proveedor_error', `GitHub no entregó access_token (${tok?.error ?? 'sin detalle'}).`);
    const h = { authorization: `Bearer ${tok.access_token}`, accept: 'application/vnd.github+json', 'user-agent': 'alerta-climatica-auth', 'x-github-api-version': '2022-11-28' };
    const user = await this.json(`${g.apiUrl}/user`, { headers: h });
    if (!user?.id) throw new OAuthError('proveedor_error', 'GitHub no devolvió el usuario.');
    let correos: any[] = [];
    try { const r = await this.json(`${g.apiUrl}/user/emails`, { headers: h }); if (Array.isArray(r)) correos = r; } catch { /* sin permiso user:email */ }
    // Solo se confía en correos que GitHub marca como verificados; el público del perfil no está verificado.
    const elegido = correos.find((e) => e.primary && e.verified) ?? correos.find((e) => e.verified);
    return { proveedor: 'github', subject: String(user.id), email: elegido?.email, emailVerificado: !!elegido, nombre: user.name || user.login };
  }

  // ------------------------------------------------------------------ utilidades
  private async json(url: string, init: RequestInit = {}): Promise<any> {
    const r = await fetch(url, { ...init, signal: AbortSignal.timeout(TIMEOUT_MS) });
    if (!r.ok) throw new Error(`HTTP ${r.status} en ${new URL(url).host}`);
    return r.json();
  }

  private setCookie(valor: string, maxAge: number) {
    return `${COOKIE}=${valor}; Max-Age=${maxAge}; Path=${COOKIE_PATH}; HttpOnly; SameSite=Lax${this.secure ? '; Secure' : ''}`;
  }
  limpiarCookie() { return this.setCookie('', 0); }
  private leerCookie(h?: string): string | undefined {
    const m = (h ?? '').split(';').map((x) => x.trim()).find((x) => x.startsWith(COOKIE + '='));
    return m ? m.slice(COOKIE.length + 1) : undefined;
  }
}
