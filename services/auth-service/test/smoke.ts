// Prueba de humo sin base de datos: repositorios simulados en memoria.
process.env.JWT_KEY = 'k'.repeat(40);
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
import { IdentidadExterna, Rol, Usuario } from '../src/entities';
import { JwtAuthGuard } from '../src/security';
import { UsersService } from '../src/users.service';

const rol = { rolId: 1, nombreRol: 'Administrador', permisos: ['reports:download'] } as Rol;
const user = { usuarioId: 7, nombre: 'Ana', correo: 'a@x.com', rolId: 1, rol, estado: true,
  contraseniaHash: bcrypt.hashSync('Secreta1', 4) } as Usuario;
const repoU = { findOne: async () => user, update: async () => undefined, existsBy: async () => true, find: async () => [user] };
const repoR = { find: async () => [rol], existsBy: async () => true, findOne: async () => rol };

(async () => {
  const mod = await Test.createTestingModule({
    imports: [JwtModule.register({})],
    controllers: [AuthController],
    providers: [UsersService, { provide: AuditClient, useValue: { registrar() {} } },
      { provide: getRepositoryToken(Usuario), useValue: repoU }, { provide: getRepositoryToken(Rol), useValue: repoR },
      { provide: getRepositoryToken(IdentidadExterna), useValue: {} }, { provide: APP_GUARD, useClass: JwtAuthGuard }],
  }).compile();
  const app = mod.createNestApplication({ bodyParser: false });
  app.use(json({ strict: false }));
  await app.listen(0);
  const base = (await app.getUrl()).replace('[::1]', 'localhost');
  const post = (p: string, body: any, h: any = {}) => fetch(base + p, { method: 'POST', headers: { 'content-type': 'application/json', ...h }, body: JSON.stringify(body) });

  assert.equal((await post('/api/auth/login', { correo: 'a@x.com', contrasenia: 'mala' })).status, 401);
  const ok = await post('/api/auth/login', { correo: 'a@x.com', contrasenia: 'Secreta1' });
  assert.equal(ok.status, 200);
  const { token, usuario } = await ok.json() as any;
  assert.equal(usuario.rol, 'Administrador');
  const claims = mod.get(JwtService).decode(token) as any;
  assert.deepEqual([claims.role, claims.permissions, claims.iss, claims.aud, claims.sub], ['Administrador', ['reports:download'], 'AlertaClimatica.Auth', 'AlertaClimatica.Client', '7']);
  assert.equal((await fetch(base + '/api/auth/roles')).status, 401);
  assert.equal((await fetch(base + '/api/auth/roles', { headers: { Authorization: 'Bearer ' + token } })).status, 200);
  const p = await fetch(base + '/api/auth/usuarios/7/estado', { method: 'PATCH', headers: { 'content-type': 'application/json', Authorization: 'Bearer ' + token }, body: 'false' });
  assert.equal(p.status, 400); // no puede desactivarse a sí mismo (y el body crudo `false` se parsea)
  console.log('auth-service smoke OK');
  await app.close();
})().catch((e) => { console.error(e); process.exit(1); });
