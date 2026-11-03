// Pruebas del Auth Service sin base de datos: repositorios simulados en memoria.
process.env.JWT_KEY = 'k'.repeat(40);
process.env.MAX_FAILED_LOGINS = '3';
import 'reflect-metadata';
import * as assert from 'assert';
import * as bcrypt from 'bcryptjs';
import { APP_GUARD } from '@nestjs/core';
import { JwtModule, JwtService } from '@nestjs/jwt';
import { Test } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { json } from 'express';
import { AuditClient } from '../src/audit.client';
import { AuthController } from '../src/auth.controller';
import { IdentidadExterna, RefreshToken, Rol, Sesion, Usuario } from '../src/entities';
import { JwtAuthGuard } from '../src/security';
import { UsersService } from '../src/users.service';

class Fake<T extends Record<string, any>> {
  rows: T[] = []; seq = 0;
  constructor(private idField: string) {}
  private match(r: T, where: any) { return !where || Object.entries(where).every(([k, v]) => typeof v === 'object' && v !== null && !(v instanceof Date) ? true : (r as any)[k] === v); }
  async findOne({ where }: any) { return this.rows.find((r) => this.match(r, where)) ?? null; }
  async find({ where }: any = {}) { return this.rows.filter((r) => this.match(r, where)); }
  create(o: any) { return { ...o }; }
  async save(o: any) {
    const list = Array.isArray(o) ? o : [o];
    for (const x of list) { if (!x[this.idField]) x[this.idField] = ++this.seq; if (!this.rows.includes(x)) this.rows.push(x); }
    return o;
  }
  async remove(list: any[]) { this.rows = this.rows.filter((r) => !list.includes(r)); }
  async update(id: any, patch: any) { const r = this.rows.find((x) => (x as any)[this.idField] === id); if (r) Object.assign(r, patch); }
  async existsBy(w: any) { return this.rows.some((r) => this.match(r, w)); }
}

const rol = { rolId: 1, nombreRol: 'Administrador', permisos: ['reports:download'] } as Rol;
const usuarios = new Fake<Usuario>('usuarioId');
usuarios.rows.push({ usuarioId: 7, nombre: 'Ana', correo: 'a@x.com', rolId: 1, rol, estado: true, intentosFallidos: 0, bloqueadoHasta: null,
  contraseniaHash: bcrypt.hashSync('Secreta123', 4) } as any);
const tokens = new Fake<RefreshToken>('id');
const sesiones = new Fake<Sesion>('id');
const roles = { find: async () => [rol], existsBy: async () => true, findOne: async () => rol };
const auditados: string[] = [];

(async () => {
  const mod = await Test.createTestingModule({
    imports: [JwtModule.register({})],
    controllers: [AuthController],
    providers: [UsersService, { provide: AuditClient, useValue: { registrar: (_u: any, a: string) => auditados.push(a) } },
      { provide: getRepositoryToken(Usuario), useValue: usuarios }, { provide: getRepositoryToken(Rol), useValue: roles },
      { provide: getRepositoryToken(IdentidadExterna), useValue: new Fake('id') }, { provide: getRepositoryToken(RefreshToken), useValue: tokens }, { provide: getRepositoryToken(Sesion), useValue: sesiones },
      { provide: APP_GUARD, useClass: JwtAuthGuard }],
  }).compile();
  const app = mod.createNestApplication({ bodyParser: false });
  app.use(json({ strict: false }));
  await app.listen(0);
  const base = (await app.getUrl()).replace('[::1]', 'localhost');
  const call = (m: string, p: string, body?: any, h: any = {}) => fetch(base + p, { method: m, headers: { 'content-type': 'application/json', ...h }, body: body === undefined ? undefined : JSON.stringify(body) });
  const login = (pw: string, correo = 'A@X.com') => call('POST', '/api/auth/login', { correo, contrasenia: pw });

  // --- login, claims y normalización de correo (mayúsculas)
  assert.equal((await login('mala')).status, 401);
  const ok = await login('Secreta123');
  assert.equal(ok.status, 200);
  const s1 = await ok.json() as any;
  assert.ok(s1.refreshToken && s1.token && s1.expiresIn === 900, 'access 15 min + refresh');
  const claims = mod.get(JwtService).decode(s1.token) as any;
  assert.deepEqual([claims.role, claims.permissions, claims.iss, claims.aud, claims.sub], ['Administrador', ['reports:download'], 'AlertaClimatica.Auth', 'AlertaClimatica.Client', '7']);
  assert.ok(claims.exp - claims.iat === 900);
  assert.ok(claims.sid, 'el access token lleva el id de sesión (sid)');
  assert.equal((await call('GET', '/api/auth/roles')).status, 401);
  assert.equal((await call('GET', '/api/auth/roles', undefined, { Authorization: 'Bearer ' + s1.token })).status, 200);

  // --- refresh con rotación y detección de reutilización
  const r1 = await call('POST', '/api/auth/refresh', { refreshToken: s1.refreshToken });
  assert.equal(r1.status, 200);
  const s2 = await r1.json() as any;
  assert.notEqual(s2.refreshToken, s1.refreshToken);
  assert.equal((await call('POST', '/api/auth/refresh', { refreshToken: s1.refreshToken })).status, 401, 'refresh usado = reutilización');
  assert.equal((await call('POST', '/api/auth/refresh', { refreshToken: s2.refreshToken })).status, 401, 'reutilización cierra todas las sesiones');
  assert.ok(auditados.includes('REFRESH_REUTILIZADO'));
  assert.equal((await call('POST', '/api/auth/refresh', { refreshToken: 'inventado' })).status, 401);

  // --- logout revoca el refresh token (y funciona sin access token)
  const s3 = await (await login('Secreta123')).json() as any;
  assert.equal((await call('POST', '/api/auth/logout', { refreshToken: s3.refreshToken })).status, 200);
  assert.equal((await call('POST', '/api/auth/refresh', { refreshToken: s3.refreshToken })).status, 401, 'tras logout no renueva');

  // --- bloqueo por intentos fallidos (MAX_FAILED_LOGINS=3)
  for (let i = 0; i < 3; i++) assert.equal((await login('x' + i)).status, 401);
  assert.equal((await login('Secreta123')).status, 429, 'cuenta bloqueada aun con la clave correcta');
  assert.ok(auditados.includes('CUENTA_BLOQUEADA'));
  usuarios.rows[0].bloqueadoHasta = null; // simula el paso del tiempo

  // --- cambio de contraseña y política
  const s4 = await (await login('Secreta123')).json() as any;
  const H = { Authorization: 'Bearer ' + s4.token };
  assert.equal((await call('POST', '/api/auth/change-password', { actual: 'mala', nueva: 'Nueva12345' }, H)).status, 401);
  assert.equal((await call('POST', '/api/auth/change-password', { actual: 'Secreta123', nueva: 'corta' }, H)).status, 400);
  assert.equal((await call('POST', '/api/auth/change-password', { actual: 'Secreta123', nueva: 'soloLetrasAqui' }, H)).status, 400);
  assert.equal((await call('POST', '/api/auth/change-password', { actual: 'Secreta123', nueva: 'Nueva12345' }, H)).status, 200);
  assert.equal((await call('POST', '/api/auth/refresh', { refreshToken: s4.refreshToken })).status, 401, 'cambiar clave cierra otras sesiones');
  assert.equal((await login('Secreta123')).status, 401);
  assert.equal((await login('Nueva12345')).status, 200);

  // --- PATCH con booleano crudo; no puede desactivarse a sí mismo
  const s5 = await (await login('Nueva12345')).json() as any;
  const p = await fetch(base + '/api/auth/usuarios/7/estado', { method: 'PATCH', headers: { 'content-type': 'application/json', Authorization: 'Bearer ' + s5.token }, body: 'false' });
  assert.equal(p.status, 400);
  // --- sesiones: el gateway consulta /internal/sessions/:sid; el logout la revoca AL INSTANTE
  const svcTok = mod.get(JwtService).sign({ sub: 'svc:gateway', role: 'Servicio' }, { secret: process.env.JWT_KEY, issuer: 'AlertaClimatica.Auth', audience: 'AlertaClimatica.Client', expiresIn: 60 });
  const S = { Authorization: 'Bearer ' + svcTok };
  const s6 = await (await login('Nueva12345')).json() as any;
  const sid6 = (mod.get(JwtService).decode(s6.token) as any).sid;
  assert.equal((await call('GET', `/api/auth/internal/sessions/${sid6}`, undefined, S)).status, 200, 'sesión activa');
  assert.equal((await call('GET', `/api/auth/internal/sessions/${sid6}`, undefined, { Authorization: 'Bearer ' + s6.token })).status, 403, 'un usuario normal no puede consultar sesiones');
  assert.equal((await call('GET', `/api/auth/internal/sessions/${sid6}`)).status, 401);
  assert.equal((await call('POST', '/api/auth/logout', {}, { Authorization: 'Bearer ' + s6.token })).status, 200); // solo con el access token
  assert.equal((await call('GET', `/api/auth/internal/sessions/${sid6}`, undefined, S)).status, 404, 'tras el logout el token deja de ser válido');
  assert.equal((await call('POST', '/api/auth/refresh', { refreshToken: s6.refreshToken })).status, 401);
  assert.equal((await call('GET', '/api/auth/internal/sessions/no-existe', undefined, S)).status, 404);

  // --- usuario desactivado => su sesión muere aunque el token no haya vencido
  const s7 = await (await login('Nueva12345')).json() as any;
  const sid7 = (mod.get(JwtService).decode(s7.token) as any).sid;
  usuarios.rows[0].estado = false;
  assert.equal((await call('GET', `/api/auth/internal/sessions/${sid7}`, undefined, S)).status, 404, 'usuario inactivo => sesión inactiva');
  usuarios.rows[0].estado = true;

  // --- registro público: queda PENDIENTE (inactivo) y no puede iniciar sesión
  const reg = await call('POST', '/api/auth/register', { nombre: 'Visitante', correo: 'V@Mail.com', contrasenia: 'Clave12345' });
  assert.equal(reg.status, 201);
  assert.equal((await login('Clave12345', 'v@mail.com')).status, 401, 'sin aprobación no entra');
  console.log('auth-service tests OK');
  await app.close();
})().catch((e) => { console.error(e); process.exit(1); });
