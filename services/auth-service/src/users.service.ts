import { BadRequestException, ConflictException, Injectable, NotFoundException, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { InjectRepository } from '@nestjs/typeorm';
import * as bcrypt from 'bcryptjs';
import { randomUUID } from 'crypto';
import { FindOptionsWhere, ILike, Repository } from 'typeorm';
import { AuditClient } from './audit.client';
import { cfg } from './config';
import { IdentidadExterna, Rol, Usuario } from './entities';
import { Principal } from './security';

const resumen = (u: Usuario) => ({
  usuarioId: u.usuarioId, nombre: u.nombre, correo: u.correo, rolId: u.rolId, rol: u.rol?.nombreRol,
  estado: u.estado, fechaCreacion: u.fechaCreacion, ultimoAcceso: u.ultimoAcceso,
});

@Injectable()
export class UsersService {
  constructor(
    @InjectRepository(Usuario) private users: Repository<Usuario>,
    @InjectRepository(Rol) private roles: Repository<Rol>,
    @InjectRepository(IdentidadExterna) private ids: Repository<IdentidadExterna>,
    private jwt: JwtService,
    private audit: AuditClient,
  ) {}

  private validar(nombre?: string, correo?: string, pass?: string | null) {
    if (!nombre?.trim()) throw new BadRequestException({ mensaje: 'El nombre es obligatorio.' });
    if (!correo?.trim()) throw new BadRequestException({ mensaje: 'El correo es obligatorio.' });
    if (pass !== null && (!pass || pass.length < 6)) {
      throw new BadRequestException({ mensaje: 'La contraseña debe tener al menos 6 caracteres.' });
    }
  }

  private async correoLibre(correo: string, exceptoId?: number) {
    const e = await this.users.findOne({ where: { correo: ILike(correo.trim()) } });
    if (e && e.usuarioId !== exceptoId) {
      throw new ConflictException({ mensaje: exceptoId ? 'Ya existe otro usuario registrado con ese correo.' : 'Ya existe un usuario registrado con ese correo.' });
    }
  }

  async listar(texto?: string, rolId?: number, estado?: boolean) {
    const where: FindOptionsWhere<Usuario>[] = [];
    const base: FindOptionsWhere<Usuario> = {};
    if (rolId) base.rolId = rolId;
    if (estado !== undefined) base.estado = estado;
    if (texto?.trim()) {
      const t = `%${texto.trim()}%`;
      where.push({ ...base, nombre: ILike(t) }, { ...base, correo: ILike(t) });
    } else where.push(base);
    return (await this.users.find({ where, order: { usuarioId: 'ASC' } })).map(resumen);
  }

  async obtener(id: number) {
    const u = await this.users.findOne({ where: { usuarioId: id } });
    if (!u) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    return resumen(u);
  }

  async registrarPublico(d: { nombre: string; correo: string; contrasenia: string }) {
    this.validar(d.nombre, d.correo, d.contrasenia);
    const rol = await this.roles.findOne({ where: { nombreRol: 'Usuario de consulta' } });
    if (!rol) throw new Error("No existe el rol 'Usuario de consulta'.");
    await this.correoLibre(d.correo);
    const u = await this.users.save(this.users.create({
      nombre: d.nombre.trim(), correo: d.correo.trim(), rolId: rol.rolId, estado: true,
      contraseniaHash: await bcrypt.hash(d.contrasenia, 10),
    }));
    return resumen((await this.users.findOne({ where: { usuarioId: u.usuarioId } }))!);
  }

  async crearAdmin(d: { nombre: string; correo: string; contrasenia: string; rolId: number }, actor: Principal) {
    this.validar(d.nombre, d.correo, d.contrasenia);
    if (!(await this.roles.existsBy({ rolId: d.rolId }))) throw new BadRequestException({ mensaje: 'El rol seleccionado no existe.' });
    await this.correoLibre(d.correo);
    const u = await this.users.save(this.users.create({
      nombre: d.nombre.trim(), correo: d.correo.trim(), rolId: d.rolId, estado: true,
      contraseniaHash: await bcrypt.hash(d.contrasenia, 10),
    }));
    this.audit.registrar(actor, 'CREAR_USUARIO', `El administrador creó el usuario "${u.nombre}" (${u.correo}) con rol Id ${u.rolId}.`, 'Usuario', u.usuarioId);
    return this.obtener(u.usuarioId);
  }

  async actualizar(id: number, d: { nombre: string; correo: string; rolId: number }, actor: Principal) {
    this.validar(d.nombre, d.correo, null);
    if (!(await this.roles.existsBy({ rolId: d.rolId }))) throw new BadRequestException({ mensaje: 'El rol seleccionado no existe.' });
    const u = await this.users.findOne({ where: { usuarioId: id } });
    if (!u) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    await this.correoLibre(d.correo, id);
    await this.users.update(id, { nombre: d.nombre.trim(), correo: d.correo.trim(), rolId: d.rolId });
    this.audit.registrar(actor, 'EDITAR_USUARIO', `El administrador modificó el usuario #${id}. Nuevo nombre: "${d.nombre.trim()}", correo: "${d.correo.trim()}", rol Id ${d.rolId}.`, 'Usuario', id);
    return this.obtener(id);
  }

  async cambiarEstado(id: number, activo: boolean, actor: Principal) {
    if (typeof activo !== 'boolean') throw new BadRequestException({ mensaje: 'El estado debe ser true o false.' });
    if (id === Number(actor.sub) && !activo) throw new BadRequestException({ mensaje: 'No puedes desactivar tu propia cuenta.' });
    if (!(await this.users.existsBy({ usuarioId: id }))) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    await this.users.update(id, { estado: activo });
    this.audit.registrar(actor, 'CAMBIAR_ESTADO_USUARIO', `El administrador cambió el estado del usuario #${id} a ${activo ? 'Activo' : 'Inactivo'}.`, 'Usuario', id);
    return this.obtener(id);
  }

  async login(d: { correo: string; contrasenia: string }) {
    if (!d?.correo?.trim()) throw new BadRequestException({ mensaje: 'El correo es obligatorio.' });
    if (!d?.contrasenia) throw new BadRequestException({ mensaje: 'La contraseña es obligatoria.' });
    const u = await this.users.findOne({ where: { correo: ILike(d.correo.trim()) } });
    const mal = new UnauthorizedException({ mensaje: 'Correo o contraseña incorrectos.' });
    if (!u || !u.contraseniaHash) throw mal;
    if (!u.estado) throw new UnauthorizedException({ mensaje: 'El usuario se encuentra desactivado.' });
    if (!(await bcrypt.compare(d.contrasenia, u.contraseniaHash))) throw mal;
    await this.users.update(u.usuarioId, { ultimoAcceso: new Date() });
    this.audit.registrar({ name: u.nombre }, 'LOGIN', `El usuario ${u.correo} inició sesión.`, undefined, undefined, u.usuarioId);
    return {
      mensaje: 'Inicio de sesión exitoso.',
      token: this.emitirToken(u),
      usuario: { usuarioId: u.usuarioId, nombre: u.nombre, correo: u.correo, rolId: u.rolId, estado: u.estado, rol: u.rol.nombreRol },
    };
  }

  /** Punto único de emisión de tokens: la Fase 4 (OIDC) reutilizará este método tras validar la identidad externa. */
  emitirToken(u: Usuario): string {
    return this.jwt.sign(
      { sub: String(u.usuarioId), email: u.correo, name: u.nombre, role: u.rol.nombreRol, rolId: u.rolId, permissions: u.rol.permisos },
      { secret: cfg.jwtKey, issuer: cfg.issuer, audience: cfg.audience, expiresIn: cfg.expirationMinutes * 60, jwtid: randomUUID() },
    );
  }

  logout(actor: Principal) {
    this.audit.registrar(actor, 'LOGOUT', 'El usuario cerró sesión.');
    return { mensaje: 'Sesión cerrada correctamente.' };
  }

  listarRoles() {
    return this.roles.find({ order: { rolId: 'ASC' } }).then((r) => r.map((x) => ({ rolId: x.rolId, nombreRol: x.nombreRol, descripcion: x.descripcion })));
  }

  async vincularIdentidad(usuarioId: number, d: { proveedor: string; subject: string }, actor: Principal) {
    if (!d?.proveedor?.trim() || !d?.subject?.trim()) throw new BadRequestException({ mensaje: 'Proveedor y subject son obligatorios.' });
    if (!(await this.users.existsBy({ usuarioId }))) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    if (await this.ids.existsBy({ proveedor: d.proveedor, subject: d.subject })) throw new ConflictException({ mensaje: 'Esa identidad externa ya está vinculada.' });
    await this.ids.save(this.ids.create({ proveedor: d.proveedor.trim(), subject: d.subject.trim(), usuarioId }));
    this.audit.registrar(actor, 'VINCULAR_IDENTIDAD', `Se vinculó la identidad ${d.proveedor} al usuario #${usuarioId}.`, 'Usuario', usuarioId);
    return { mensaje: 'Identidad vinculada.' };
  }
}
