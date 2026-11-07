// Pruebas del login OAuth 2.0 / OIDC (Google + GitHub) contra proveedores simulados en local. Sin base de datos ni internet.
process.env.JWT_KEY = 'k'.repeat(40);
process.env.PUBLIC_URL = 'https://miguate.lat';
import 'reflect-metadata';
import * as assert from 'assert';
import { createHash, createSign, generateKeyPairSync } from 'crypto';
import { createServer } from 'http';
import { APP_GUARD } from '@nestjs/core';
import { JwtModule, JwtService } from '@nestjs/jwt';
import { Test } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { json } from 'express';
import { AuditClient } from '../src/audit.client';
import { AuthController, OAuthController } from '../src/auth.controller';
import { cfg } from '../src/config';
import { IdentidadExterna, RefreshToken, Rol, Sesion, Usuario } from '../src/entities';
import { OAuthService } from '../src/oauth.service';
import { JwtAuthGuard } from '../src/security';
import { UsersService } from '../src/users.service';

class Fake<T extends Record<string, any>> {
  rows: T[] = []; seq = 0;
  constructor(private idField: string, private onSave?: (x: any) => void) {}
  private match(r: T, where: any) { return !where || Object.entries(where).every(([k, v]) => typeof v === 'object' && v !== null && !(v instanceof Date) ? true : (r as any)[k] === v); }
  async findOne({ where }: any) { return this.rows.find((r) => this.match(r, where)) ?? null; }
  async find({ where }: any = {}) { return this.rows.filter((r) => this.match(r, where)); }
  create(o: any) { return { ...o }; }
  async save(o: any) {
    for (const x of Array.isArray(o) ? o : [o]) { if (!x[this.idField]) x[this.idField] = ++this.seq; this.onSave?.(x); if (!this.rows.includes(x)) this.rows.push(x); }
    return o;
  }
  async remove(list: any[]) { this.rows = this.rows.filter((r) => !list.includes(r)); }
  async update(id: any, patch: any) { const r = this.rows.find((x) => (x as any)[this.idField] === id); if (r) Object.assign(r, patch); }
  async existsBy(w: any) { return this.rows.some((r) => this.match(r, w)); }
}

const b64u = (x: Buffer | string) => Buffer.from(x).toString('base64url');
const rolConsulta = { rolId: 3, nombreRol: 'Usuario de consulta', permisos: [] as string[], descripcion: null } as Rol;
const rolAdmin = { rolId: 1, nombreRol: 'Administrador', permisos: ['reports:download'], descripcion: null } as Rol;

(async () => {
  // ---------------------------------------------------------------- proveedor simulado (Google OIDC + GitHub)
  const { privateKey, publicKey } = generateKeyPairSync('rsa', { modulusLength: 2048 });
  const otra = generateKeyPairSync('rsa', { modulusLength: 2048 });
  const jwk = { ...publicKey.export({ format: 'jwk' }), kid: 'k1', alg: 'RS256', use: 'sig' };
  const firmar = (claims: any, key = privateKey, header: any = { alg: 'RS256', kid: 'k1', typ: 'JWT' }) => {
    const h = b64u(JSON.stringify(header)), p = b64u(JSON.stringify(claims));
    return `${h}.${p}.${key ? createSign('RSA-SHA256').update(`${h}.${p}`).sign(key).toString('base64url') : ''}`;
  };

  let base = '';
  let authReq: URLSearchParams = new URLSearchParams();          // lo que la app envió al "proveedor"
  let google: { sub: string; email?: string; email_verified?: any; name?: string; mod?: (c: any) => any; firma?: any; header?: any } = { sub: 'g-1' };
  let gh = { id: 4242, login: 'octo', name: 'Octo Cat', emails: [{ email: 'octo@x.com', primary: true, verified: true }] as any[] };
  const secretsRecibidos: string[] = [];

  const srv = createServer((req, res) => {
    const url = new URL(req.url!, base || 'http://x');
    const send = (o: any, st = 200) => { res.writeHead(st, { 'content-type': 'application/json' }); res.end(JSON.stringify(o)); };
    let body = '';
    req.on('data', (d) => (body += d));
    req.on('end', () => {
      const form = new URLSearchParams(body);
      if (url.pathname === '/google/.well-known/openid-configuration')
        return send({ issuer: 'https://accounts.google.com', authorization_endpoint: base + '/google/auth', token_endpoint: base + '/google/token', jwks_uri: base + '/google/jwks' });
      if (url.pathname === '/google/jwks') return send({ keys: [jwk] });
      if (url.pathname === '/google/token') {
        secretsRecibidos.push(form.get('client_secret') ?? '');
        // PKCE: el verifier recibido debe corresponder al challenge enviado al autorizar
        const ch = createHash('sha256').update(form.get('code_verifier') ?? '').digest('base64url');
        if (form.get('client_secret') !== 'gsecret' || ch !== authReq.get('code_challenge') || form.get('redirect_uri') !== 'https://miguate.lat/api/auth/oauth/google/callback') return send({ error: 'invalid_grant' }, 400);
        const ahora = Math.floor(Date.now() / 1000);
        let claims: any = { iss: 'https://accounts.google.com', aud: 'gid', sub: google.sub, email: google.email, email_verified: google.email_verified ?? true, name: google.name, nonce: authReq.get('nonce'), iat: ahora, exp: ahora + 300 };
        if (google.mod) claims = google.mod(claims);
        return send({ id_token: firmar(claims, google.firma ?? privateKey, google.header), access_token: 'x' });
      }
      if (url.pathname === '/gh/token') {
        secretsRecibidos.push(form.get('client_secret') ?? '');
        const ch = createHash('sha256').update(form.get('code_verifier') ?? '').digest('base64url');
        if (form.get('client_secret') !== 'hsecret' || ch !== authReq.get('code_challenge')) return send({ error: 'bad_verification_code' });
        return send({ access_token: 'gho_test', token_type: 'bearer' });
      }
      if (req.headers.authorization !== 'Bearer gho_test') return send({ message: 'Bad credentials' }, 401);
      if (url.pathname === '/gh/user') return send({ id: gh.id, login: gh.login, name: gh.name });
      if (url.pathname === '/gh/user/emails') return send(gh.emails);
      send({}, 404);
    });
  });
  await new Promise<void>((r) => srv.listen(0, '127.0.0.1', r));
  base = `http://127.0.0.1:${(srv.address() as any).port}`;

  Object.assign(cfg.oauth.google, { clientId: 'gid', clientSecret: 'gsecret', discoveryUrl: base + '/google/.well-known/openid-configuration' });
  Object.assign(cfg.oauth.github, { clientId: 'hid', clientSecret: 'hsecret', authorizeUrl: base + '/gh/auth', tokenUrl: base + '/gh/token', apiUrl: base + '/gh' });

  // ---------------------------------------------------------------- app de Auth con repositorios en memoria
  const usuarios = new Fake<Usuario>('usuarioId', (x) => { x.rol = x.rolId === 1 ? rolAdmin : rolConsulta; });
  usuarios.rows.push({ usuarioId: 7, nombre: 'Ana', correo: 'ana@x.com', rolId: 1, rol: rolAdmin, estado: true, intentosFallidos: 0, bloqueadoHasta: null, contraseniaHash: 'hash' } as any);
  const identidades = new Fake<IdentidadExterna>('id');
  const tokens = new Fake<RefreshToken>('id');
  const sesiones = new Fake<Sesion>('id');
  const roles = { find: async () => [rolAdmin, rolConsulta], existsBy: async () => true, findOne: async () => rolConsulta };
  const auditados: string[] = [];

  const mod = await Test.createTestingModule({
    imports: [JwtModule.register({})],
    controllers: [AuthController, OAuthController],
    providers: [UsersService, OAuthService, { provide: AuditClient, useValue: { registrar: (_u: any, a: string) => auditados.push(a) } },
      { provide: getRepositoryToken(Usuario), useValue: usuarios }, { provide: getRepositoryToken(Rol), useValue: roles },
      { provide: getRepositoryToken(IdentidadExterna), useValue: identidades }, { provide: getRepositoryToken(RefreshToken), useValue: tokens },
      { provide: getRepositoryToken(Sesion), useValue: sesiones }, { provide: APP_GUARD, useClass: JwtAuthGuard }],
  }).compile();
  const app = mod.createNestApplication({ bodyParser: false });
  app.use(json({ strict: false }));
  await app.listen(0);
  const api = (await app.getUrl()).replace('[::1]', 'localhost');
  const get = (p: string, h: any = {}) => fetch(api + p, { redirect: 'manual', headers: h });

  /** Paso 1: /start -> devuelve parámetros enviados al proveedor y la cookie de contexto. */
  async function start(prov: string) {
    const r = await get(`/api/auth/oauth/${prov}/start`);
    assert.equal(r.status, 302);
    const loc = new URL(r.headers.get('location')!);
    authReq = loc.searchParams;
    const set = r.headers.get('set-cookie') ?? '';
    return { loc, set, cookie: set.split(';')[0], state: loc.searchParams.get('state')! };
  }
  /** Paso 2: callback del proveedor. */
  async function callback(prov: string, state: string | null, cookie?: string, extra = 'code=abc') {
    const r = await get(`/api/auth/oauth/${prov}/callback?${extra}${state ? '&state=' + encodeURIComponent(state) : ''}`, cookie ? { cookie } : {});
    assert.equal(r.status, 302);
    return r.headers.get('location')!;
  }
  const flujo = async (prov: string) => { const s = await start(prov); return callback(prov, s.state, s.cookie); };
  const errorDe = (loc: string) => new URL(loc, 'http://x').searchParams.get('oauth_error');
  const canjear = async (loc: string) => {
    assert.ok(loc.startsWith('/oauth/callback#rt='), 'redirige al frontend con el ticket en el fragmento: ' + loc);
    const rt = decodeURIComponent(loc.split('#rt=')[1]);
    const r = await fetch(api + '/api/auth/refresh', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ refreshToken: rt }) });
    return { status: r.status, rt, body: (await r.json()) as any };
  };

  // ================================================================ 1) proveedores y /start
  const prov = (await (await get('/api/auth/oauth/providers')).json()) as any;
  assert.deepEqual(prov.providers, ['google', 'github'], 'ambos proveedores habilitados');

  const sg = await start('google');
  assert.equal(sg.loc.origin + sg.loc.pathname, base + '/google/auth');
  const q = sg.loc.searchParams;
  assert.equal(q.get('response_type'), 'code');
  assert.equal(q.get('client_id'), 'gid');
  assert.equal(q.get('redirect_uri'), 'https://miguate.lat/api/auth/oauth/google/callback');
  assert.ok(q.get('scope')!.includes('openid') && q.get('scope')!.includes('email'));
  assert.equal(q.get('code_challenge_method'), 'S256');
  assert.ok(q.get('state')!.length >= 24 && q.get('nonce') && q.get('code_challenge'));
  assert.ok(!sg.loc.toString().includes('gsecret'), 'el client_secret nunca va al navegador');
  assert.ok(/HttpOnly/.test(sg.set) && /SameSite=Lax/.test(sg.set) && /Secure/.test(sg.set) && /Path=\/api\/auth\/oauth/.test(sg.set), 'cookie de contexto endurecida: ' + sg.set);
  const sh = await start('github');
  assert.equal(sh.loc.origin + sh.loc.pathname, base + '/gh/auth');
  assert.ok(sh.loc.searchParams.get('scope')!.includes('user:email'));

  // ================================================================ 2) Google: cuenta nueva -> pendiente de aprobación
  google = { sub: 'g-1', email: 'Nuevo@Gmail.com', name: 'Nuevo Usuario' };
  assert.equal(errorDe(await flujo('google')), 'cuenta_inactiva', 'cuenta nueva = pendiente (política del proyecto)');
  const nuevo = usuarios.rows.find((u) => u.correo === 'nuevo@gmail.com')!;
  assert.ok(nuevo && nuevo.estado === false && nuevo.rol.nombreRol === 'Usuario de consulta' && nuevo.contraseniaHash === '', 'rol mínimo, inactiva y sin contraseña local');
  assert.equal(identidades.rows.length, 1);
  assert.ok(auditados.includes('REGISTRO_PENDIENTE') && auditados.includes('LOGIN_OAUTH_FALLIDO'));

  // Tras la aprobación del administrador -> login completo y JWT propio
  nuevo.estado = true;
  const ok = await canjear(await flujo('google'));
  assert.equal(ok.status, 200);
  const c = mod.get(JwtService).decode(ok.body.token) as any;
  assert.deepEqual([c.role, c.iss, c.aud, c.email, !!c.sid], ['Usuario de consulta', 'AlertaClimatica.Auth', 'AlertaClimatica.Client', 'nuevo@gmail.com', true]);
  assert.equal(c.exp - c.iat, 900);
  assert.equal(identidades.rows.length, 1, 'la identidad ya vinculada no se duplica');
  assert.ok(auditados.includes('LOGIN_OAUTH'));
  // El ticket es de un solo uso (refresh rotativo): reutilizarlo falla
  const reuso = await fetch(api + '/api/auth/refresh', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ refreshToken: ok.rt }) });
  assert.equal(reuso.status, 401, 'el ticket #rt no se puede reutilizar');
  // el JWT emitido funciona contra el propio guard
  assert.equal((await fetch(api + '/api/auth/me', { headers: { authorization: 'Bearer ' + ok.body.token } })).status, 200);

  // ================================================================ 3) GitHub: vincula por correo verificado a una cuenta existente
  gh = { id: 4242, login: 'octo', name: 'Octo', emails: [{ email: 'x@priv.com', primary: false, verified: false }, { email: 'ANA@x.com', primary: true, verified: true }] };
  const g1 = await canjear(await flujo('github'));
  assert.equal(g1.status, 200);
  assert.equal((mod.get(JwtService).decode(g1.body.token) as any).sub, '7', 'entra como la cuenta existente de Ana');
  assert.ok(identidades.rows.some((i) => i.proveedor === 'github' && i.subject === '4242' && i.usuarioId === 7), 'identidad vinculada');
  assert.equal(usuarios.rows.length, 2, 'no se creó un usuario duplicado');
  const antes = identidades.rows.length;
  assert.equal((await canjear(await flujo('github'))).status, 200);
  assert.equal(identidades.rows.length, antes, 'segundo login reconoce (github, subject)');

  // GitHub sin ningún correo verificado -> rechazo (no se puede enlazar por correo no verificado)
  gh = { id: 999, login: 'sin', name: 'Sin Correo', emails: [{ email: 'ana@x.com', primary: true, verified: false }] };
  assert.equal(errorDe(await flujo('github')), 'email_no_verificado');
  assert.equal(identidades.rows.some((i) => i.subject === '999'), false);

  // Google con email_verified=false que coincide con Ana (intento de toma de cuenta)
  google = { sub: 'g-evil', email: 'ana@x.com', email_verified: false };
  assert.equal(errorDe(await flujo('google')), 'email_no_verificado');

  // ================================================================ 4) ataques al flujo
  google = { sub: 'g-1', email: 'nuevo@gmail.com' };
  // 4.1 state manipulado
  let s = await start('google');
  assert.equal(errorDe(await callback('google', 'otro-state', s.cookie)), 'estado_invalido');
  // 4.2 sin cookie (login CSRF: otro navegador intenta usar un code ajeno)
  s = await start('google');
  assert.equal(errorDe(await callback('google', s.state, undefined)), 'estado_invalido');
  // 4.3 cookie firmada con otra clave / alterada
  s = await start('google');
  assert.equal(errorDe(await callback('google', s.state, s.cookie.slice(0, -3) + 'abc')), 'estado_invalido');
  // 4.4 cookie de Google usada en el callback de GitHub
  s = await start('google');
  assert.equal(errorDe(await callback('github', s.state, s.cookie)), 'estado_invalido');
  // 4.5 el usuario cancela en el proveedor
  s = await start('google');
  assert.equal(errorDe(await callback('google', s.state, s.cookie, 'error=access_denied')), 'acceso_denegado');
  // 4.6 id_token: nonce distinto (repetición), aud ajena, iss falso, expirado, firmado con otra clave, alg=none
  const malos: [string, typeof google][] = [
    ['nonce', { ...google, mod: (c) => ({ ...c, nonce: 'repetido' }) }],
    ['aud', { ...google, mod: (c) => ({ ...c, aud: 'otra-app' }) }],
    ['iss', { ...google, mod: (c) => ({ ...c, iss: 'https://evil.example' }) }],
    ['exp', { ...google, mod: (c) => ({ ...c, exp: Math.floor(Date.now() / 1000) - 3600 }) }],
    ['firma', { ...google, firma: otra.privateKey }],
    ['alg none', { ...google, firma: null, header: { alg: 'none', kid: 'k1' } }],
  ];
  for (const [nombre, cfgMalo] of malos) {
    google = cfgMalo;
    assert.equal(errorDe(await flujo('google')), 'proveedor_error', `id_token inválido (${nombre}) debe rechazarse`);
  }
  google = { sub: 'g-1', email: 'nuevo@gmail.com' };
  // 4.7 el state de un intento no sirve dos veces (la cookie se borra, pero además un callback repetido sin cookie falla)
  s = await start('google');
  const l1 = await callback('google', s.state, s.cookie);
  assert.ok(l1.startsWith('/oauth/callback#rt='));
  assert.equal(errorDe(await callback('google', s.state, undefined)), 'estado_invalido');
  const limp = await get(`/api/auth/oauth/google/callback?code=abc&state=${s.state}`, { cookie: s.cookie });
  assert.match(limp.headers.get('set-cookie') ?? '', /Max-Age=0/, 'la cookie de contexto se invalida tras el callback');
  // 4.8 el proveedor rechaza el code (secreto/redirect incorrecto) -> error genérico, sin filtrar detalle
  cfg.oauth.google.clientSecret = 'incorrecto';
  assert.equal(errorDe(await flujo('google')), 'proveedor_error');
  cfg.oauth.google.clientSecret = 'gsecret';

  // ================================================================ 5) proveedor deshabilitado / desconocido
  assert.equal(errorDe((await get('/api/auth/oauth/facebook/start')).headers.get('location')!), 'proveedor_no_disponible');
  cfg.oauth.github.clientSecret = ''; cfg.oauth.github.clientId = '';
  assert.deepEqual(((await (await get('/api/auth/oauth/providers')).json()) as any).providers, ['google']);
  assert.equal(errorDe((await get('/api/auth/oauth/github/start')).headers.get('location')!), 'proveedor_no_disponible');

  // ================================================================ 6) sin filtración de secretos
  assert.ok(secretsRecibidos.every((x) => ['gsecret', 'hsecret', 'incorrecto'].includes(x)), 'el secreto solo se envía al token endpoint (servidor a servidor)');

  await app.close(); srv.close();
  console.log('oauth tests OK');
})().catch((e) => { console.error(e); process.exit(1); });
