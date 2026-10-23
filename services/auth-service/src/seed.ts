import { Logger } from '@nestjs/common';
import * as bcrypt from 'bcryptjs';
import { DataSource } from 'typeorm';
import { Rol, Usuario } from './entities';

const ROLES = [
  { nombreRol: 'Administrador', descripcion: 'Acceso total al sistema y gestión de usuarios',
    permisos: ['users:manage', 'audit:read', 'reports:generate', 'reports:history', 'reports:download'] },
  { nombreRol: 'Operador', descripcion: 'Gestión y control de sensores y alertas', permisos: ['reports:generate', 'reports:history'] },
  { nombreRol: 'Usuario de consulta', descripcion: 'Acceso únicamente a dashboards y consulta', permisos: [] as string[] },
];

export async function seed(ds: DataSource) {
  const log = new Logger('Seed');
  const roles = ds.getRepository(Rol);
  for (const r of ROLES) {
    const e = await roles.findOne({ where: { nombreRol: r.nombreRol } });
    if (!e) await roles.save(roles.create(r));
    else if (JSON.stringify(e.permisos) !== JSON.stringify(r.permisos)) await roles.update(e.rolId, { permisos: r.permisos });
  }
  const users = ds.getRepository(Usuario);
  if ((await users.count()) > 0) return;
  const pass = process.env.ADMIN_PASSWORD;
  if (!pass) throw new Error('Base vacía: defina ADMIN_PASSWORD para crear el administrador inicial.');
  const admin = await roles.findOneByOrFail({ nombreRol: 'Administrador' });
  await users.save(users.create({ nombre: 'Administrador Del Sistema', correo: 'admin@alerta.com', rolId: admin.rolId, estado: true,
    contraseniaHash: await bcrypt.hash(pass, 10) }));
  log.log('Administrador inicial creado (admin@alerta.com).');
}
