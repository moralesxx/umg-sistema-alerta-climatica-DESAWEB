import { BadRequestException, ConflictException, HttpException, Injectable, NotFoundException, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { InjectRepository } from '@nestjs/typeorm';
import * as bcrypt from 'bcryptjs';
import { createHash, randomBytes, randomUUID } from 'crypto';
import { FindOptionsWhere, ILike, Repository } from 'typeorm';
import { AuditClient } from './audit.client';
import { cfg } from './config';
import { IdentidadExterna, RefreshToken, Rol, Sesion, Usuario } from './entities';
import { OAuthError, PerfilExterno } from './oauth.service';
import { Principal } from './security';

const resumen = (u: Usuario) => ({
  usuarioId: u.usuarioId, nombre: u.nombre, correo: u.correo, rolId: u.rolId, rol: u.rol?.nombreRol,
  estado: u.estado, fechaCreacion: u.fechaCreacion, ultimoAcceso: u.ultimoAcceso,
});

const sha256 = (s: string) => createHash('sha256').update(s).digest('hex');
const normalizarCorreo = (c: string) => c.trim().toLowerCase();
const escaparLike = (t: string) => t.replace(/[\\%_]/g, (m) => '\\' + m);
// Hash de relleno: se compara aunque el usuario no exista, para que el tiempo de respuesta no revele qué correos existen.
const HASH_RELLENO = bcrypt.hashSync('relleno-no-valido', 10);

/** Política: 8–72 caracteres (límite de bcrypt), al menos una letra y un número. */
export function validarPolitica(p?: string | null) {
  if (!p || p.length < 8) throw new BadRequestException({ mensaje: 'La contraseña debe tener al menos 8 caracteres.' });
  if (Buffer.byteLength(p) > 72) throw new BadRequestException({ mensaje: 'La contraseña no puede superar 72 caracteres.' });
  if (!/[A-Za-z]/.test(p) || !/\d/.test(p)) throw new BadRequestException({ mensaje: 'La contraseña debe incluir letras y números.' });
}

@Injectable()
export class UsersService {
  constructor(
    @InjectRepository(Usuario) private users: Repository<Usuario>,
    @InjectRepository(Rol) private roles: Repository<Rol>,
    @InjectRepository(IdentidadExterna) private ids: Repository<IdentidadExterna>,
    @InjectRepository(RefreshToken) private tokens: Repository<RefreshToken>,
    @InjectRepository(Sesion) private sesiones: Repository<Sesion>,
    private jwt: JwtService,
    private audit: AuditClient,
  ) {}

  private validar(nombre?: string, correo?: string) {
    if (!nombre?.trim()) throw new BadRequestException({ mensaje: 'El nombre es obligatorio.' });
    if (!correo?.trim()) throw new BadRequestException({ mensaje: 'El correo es obligatorio.' });
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo.trim())) throw new BadRequestException({ mensaje: 'El correo no tiene un formato válido.' });
  }

  private async correoLibre(correo: string, exceptoId?: number) {
    const e = await this.users.findOne({ where: { correo: normalizarCorreo(correo) } });
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
      const t = `%${escaparLike(texto.trim())}%`;
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
    this.validar(d?.nombre, d?.correo);
    validarPolitica(d.contrasenia);
    const rol = await this.roles.findOne({ where: { nombreRol: 'Usuario de consulta' } });
    if (!rol) throw new Error("No existe el rol 'Usuario de consulta'.");
    await this.correoLibre(d.correo);
    // Seguridad: un registro público NO da acceso; la cuenta queda inactiva hasta que un administrador la apruebe.
    const u = await this.users.save(this.users.create({
      nombre: d.nombre.trim(), correo: normalizarCorreo(d.correo), rolId: rol.rolId, estado: false,
      contraseniaHash: await bcrypt.hash(d.contrasenia, 10),
    }));
    this.audit.registrar({ name: u.nombre }, 'REGISTRO_PENDIENTE', `Solicitud de acceso de ${u.correo}; pendiente de aprobación.`, 'Usuario', u.usuarioId, u.usuarioId);
    return { mensaje: 'Solicitud registrada. Un administrador debe activar su cuenta antes de iniciar sesión.', ...resumen((await this.users.findOne({ where: { usuarioId: u.usuarioId } }))!) };
  }

  async crearAdmin(d: { nombre: string; correo: string; contrasenia: string; rolId: number }, actor: Principal) {
    this.validar(d?.nombre, d?.correo);
    validarPolitica(d.contrasenia);
    if (!(await this.roles.existsBy({ rolId: d.rolId }))) throw new BadRequestException({ mensaje: 'El rol seleccionado no existe.' });
    await this.correoLibre(d.correo);
    const u = await this.users.save(this.users.create({
      nombre: d.nombre.trim(), correo: normalizarCorreo(d.correo), rolId: d.rolId, estado: true,
      contraseniaHash: await bcrypt.hash(d.contrasenia, 10),
    }));
    this.audit.registrar(actor, 'CREAR_USUARIO', `El administrador creó el usuario "${u.nombre}" (${u.correo}) con rol Id ${u.rolId}.`, 'Usuario', u.usuarioId);
    return this.obtener(u.usuarioId);
  }

  async actualizar(id: number, d: { nombre: string; correo: string; rolId: number }, actor: Principal) {
    this.validar(d?.nombre, d?.correo);
    if (!(await this.roles.existsBy({ rolId: d.rolId }))) throw new BadRequestException({ mensaje: 'El rol seleccionado no existe.' });
    const u = await this.users.findOne({ where: { usuarioId: id } });
    if (!u) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    await this.correoLibre(d.correo, id);
    await this.users.update(id, { nombre: d.nombre.trim(), correo: normalizarCorreo(d.correo), rolId: d.rolId });
    if (u.rolId !== d.rolId) await this.revocarTodos(id); // el token lleva el rol: se fuerza un nuevo login con el rol actualizado
    this.audit.registrar(actor, 'EDITAR_USUARIO', `El administrador modificó el usuario #${id}. Nuevo nombre: "${d.nombre.trim()}", correo: "${normalizarCorreo(d.correo)}", rol Id ${d.rolId}.`, 'Usuario', id);
    return this.obtener(id);
  }

  async cambiarEstado(id: number, activo: boolean, actor: Principal) {
    if (typeof activo !== 'boolean') throw new BadRequestException({ mensaje: 'El estado debe ser true o false.' });
    if (id === Number(actor.sub) && !activo) throw new BadRequestException({ mensaje: 'No puedes desactivar tu propia cuenta.' });
    if (!(await this.users.existsBy({ usuarioId: id }))) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    await this.users.update(id, { estado: activo });
    if (!activo) await this.revocarTodos(id); // un usuario desactivado no puede renovar su sesión
    this.audit.registrar(actor, 'CAMBIAR_ESTADO_USUARIO', `El administrador cambió el estado del usuario #${id} a ${activo ? 'Activo' : 'Inactivo'}.`, 'Usuario', id);
    return this.obtener(id);
  }

  // ------------------------------------------------------------------ sesión
  async login(d: { correo: string; contrasenia: string }) {
    if (!d?.correo?.trim()) throw new BadRequestException({ mensaje: 'El correo es obligatorio.' });
    if (!d?.contrasenia) throw new BadRequestException({ mensaje: 'La contraseña es obligatoria.' });
    const u = await this.users.findOne({ where: { correo: normalizarCorreo(d.correo) } });
    const mal = new UnauthorizedException({ mensaje: 'Correo o contraseña incorrectos.' });
    const ahora = new Date();

    if (u?.bloqueadoHasta && u.bloqueadoHasta > ahora) {
      throw new HttpException({ mensaje: 'La cuenta está bloqueada temporalmente por intentos fallidos. Intente de nuevo más tarde.' }, 429);
    }
    const ok = await bcrypt.compare(d.contrasenia, u?.contraseniaHash || HASH_RELLENO); // siempre se compara (tiempo constante)
    if (!u || !u.contraseniaHash) throw mal;
    if (!ok) {
      const intentos = (u.intentosFallidos ?? 0) + 1;
      if (intentos >= cfg.maxFailedLogins) {
        await this.users.update(u.usuarioId, { intentosFallidos: 0, bloqueadoHasta: new Date(ahora.getTime() + cfg.lockMinutes * 60_000) });
        this.audit.registrar({ name: u.nombre }, 'CUENTA_BLOQUEADA', `Cuenta ${u.correo} bloqueada ${cfg.lockMinutes} min por ${cfg.maxFailedLogins} intentos fallidos.`, 'Usuario', u.usuarioId, u.usuarioId);
      } else {
        await this.users.update(u.usuarioId, { intentosFallidos: intentos });
      }
      throw mal;
    }
    if (!u.estado) throw new UnauthorizedException({ mensaje: 'La cuenta está desactivada o pendiente de aprobación por un administrador.' });
    await this.users.update(u.usuarioId, { ultimoAcceso: ahora, intentosFallidos: 0, bloqueadoHasta: null });
    this.audit.registrar({ name: u.nombre }, 'LOGIN', `El usuario ${u.correo} inició sesión.`, undefined, undefined, u.usuarioId);
    return { mensaje: 'Inicio de sesión exitoso.', ...(await this.emitirSesion(u)) };
  }

  // ------------------------------------------------------------------ login externo (OAuth 2.0 / OIDC)
  /**
   * Convierte una identidad ya verificada por Google/GitHub en una sesión PROPIA del sistema (mismo JWT, `sid` y refresh que el login local).
   *  1) (proveedor, subject) ya vinculado            -> ese usuario.
   *  2) correo verificado igual al de una cuenta     -> se vincula automáticamente (auditado).
   *  3) ninguna coincidencia                         -> se crea una cuenta de rol mínimo ('Usuario de consulta'),
   *                                                     pendiente de aprobación salvo OAUTH_AUTO_ACTIVATE=true.
   * Nunca se enlaza por un correo que el proveedor no haya verificado (evita toma de cuentas).
   */
  async accederConIdentidadExterna(p: PerfilExterno) {
    let u: Usuario | null = null;
    const ident = await this.ids.findOne({ where: { proveedor: p.proveedor, subject: p.subject } });
    if (ident) u = await this.users.findOne({ where: { usuarioId: ident.usuarioId } });

    if (!u) {
      if (!p.email || !p.emailVerificado) throw new OAuthError('email_no_verificado', 'El proveedor no entregó un correo verificado.');
      const correo = normalizarCorreo(p.email);
      u = await this.users.findOne({ where: { correo } });
      if (u) {
        this.audit.registrar({ name: u.nombre }, 'VINCULAR_IDENTIDAD', `Se vinculó automáticamente la identidad ${p.proveedor} a ${u.correo} (correo verificado por el proveedor).`, 'Usuario', u.usuarioId, u.usuarioId);
      } else {
        const rol = await this.roles.findOne({ where: { nombreRol: 'Usuario de consulta' } });
        if (!rol) throw new Error("No existe el rol 'Usuario de consulta'.");
        const nombre = (p.nombre?.trim() || correo.split('@')[0]).slice(0, 120);
        u = await this.users.save(this.users.create({ nombre, correo, rolId: rol.rolId, estado: cfg.oauth.autoActivate, contraseniaHash: '' }));
        u = (await this.users.findOne({ where: { usuarioId: u.usuarioId } })) ?? u; // recarga con el rol
        this.audit.registrar({ name: u.nombre }, cfg.oauth.autoActivate ? 'REGISTRO_OAUTH' : 'REGISTRO_PENDIENTE',
          `Cuenta creada por ${p.proveedor} para ${u.correo}${cfg.oauth.autoActivate ? '.' : '; pendiente de aprobación.'}`, 'Usuario', u.usuarioId, u.usuarioId);
      }
      await this.ids.save(this.ids.create({ proveedor: p.proveedor, subject: p.subject, usuarioId: u.usuarioId }));
    }

    if (!u.estado) throw new OAuthError('cuenta_inactiva', `Cuenta ${u.correo} inactiva o pendiente de aprobación.`);
    await this.users.update(u.usuarioId, { ultimoAcceso: new Date(), intentosFallidos: 0, bloqueadoHasta: null });
    this.audit.registrar({ name: u.nombre }, 'LOGIN_OAUTH', `El usuario ${u.correo} inició sesión con ${p.proveedor}.`, undefined, undefined, u.usuarioId);
    return { mensaje: 'Inicio de sesión exitoso.', ...(await this.emitirSesion(u)) };
  }

  /** Emite el par access token (corto, con `sid`) + refresh token (opaco, rotativo). Punto único: la Fase 4 (OIDC) lo reutiliza. */
  private async emitirSesion(u: Usuario, sesionId?: string) {
    const ahora = new Date();
    const expira = new Date(ahora.getTime() + cfg.refreshDays * 86_400_000);
    let sid = sesionId;
    if (!sid) {
      sid = randomUUID();
      await this.sesiones.save(this.sesiones.create({ id: sid, usuarioId: u.usuarioId, expiraEn: expira, revocadaEn: null }));
    }
    const refresh = randomBytes(48).toString('base64url');
    const vencidos = (await this.tokens.find({ where: { usuarioId: u.usuarioId } })).filter((t) => t.expiraEn < ahora);
    if (vencidos.length) await this.tokens.remove(vencidos);
    await this.tokens.save(this.tokens.create({ tokenHash: sha256(refresh), usuarioId: u.usuarioId, sesionId: sid, expiraEn: expira, revocadoEn: null }));
    return {
      token: this.emitirToken(u, sid), refreshToken: refresh, expiresIn: cfg.expirationMinutes * 60,
      usuario: { usuarioId: u.usuarioId, nombre: u.nombre, correo: u.correo, rolId: u.rolId, estado: u.estado, rol: u.rol.nombreRol },
    };
  }

  emitirToken(u: Usuario, sid: string): string {
    return this.jwt.sign(
      { sub: String(u.usuarioId), sid, email: u.correo, name: u.nombre, role: u.rol.nombreRol, rolId: u.rolId, permissions: u.rol.permisos },
      { secret: cfg.jwtKey, issuer: cfg.issuer, audience: cfg.audience, expiresIn: cfg.expirationMinutes * 60, jwtid: randomUUID() },
    );
  }

  /** ¿La sesión sigue vigente? Lo consulta el gateway (endpoint interno) en cada petición autenticada. */
  async sesionActiva(sid: string): Promise<boolean> {
    const se = await this.sesiones.findOne({ where: { id: sid } });
    if (!se || se.revocadaEn || se.expiraEn < new Date()) return false;
    const u = await this.users.findOne({ where: { usuarioId: se.usuarioId } });
    return !!u && u.estado;
  }

  private async revocarSesion(sid: string | null | undefined) {
    if (!sid) return;
    const ahora = new Date();
    const se = await this.sesiones.findOne({ where: { id: sid } });
    if (se && !se.revocadaEn) { se.revocadaEn = ahora; await this.sesiones.save(se); }
    const rts = (await this.tokens.find({ where: { sesionId: sid } })).filter((t) => !t.revocadoEn);
    rts.forEach((t) => (t.revocadoEn = ahora));
    if (rts.length) await this.tokens.save(rts);
  }

  /** Renueva la sesión con rotación: el refresh usado se revoca; si se reutiliza uno ya revocado se asume robo y se cierra la sesión. */
  async refrescar(refreshToken?: string) {
    if (!refreshToken) throw new BadRequestException({ mensaje: 'Falta el refresh token.' });
    const row = await this.tokens.findOne({ where: { tokenHash: sha256(refreshToken) } });
    const invalida = new UnauthorizedException({ mensaje: 'La sesión no es válida. Inicie sesión de nuevo.' });
    if (!row) throw invalida;
    if (row.revocadoEn) {
      await this.revocarSesion(row.sesionId);
      await this.revocarTodos(row.usuarioId);
      this.audit.registrar(null, 'REFRESH_REUTILIZADO', `Se intentó reutilizar un refresh token revocado del usuario #${row.usuarioId}; se cerraron sus sesiones.`, 'Usuario', row.usuarioId, row.usuarioId);
      throw invalida;
    }
    if (row.expiraEn < new Date()) throw new UnauthorizedException({ mensaje: 'La sesión expiró. Inicie sesión de nuevo.' });
    if (row.sesionId && !(await this.sesionActiva(row.sesionId))) throw invalida;
    const u = await this.users.findOne({ where: { usuarioId: row.usuarioId } });
    if (!u || !u.estado) throw invalida;
    row.revocadoEn = new Date();
    await this.tokens.save(row);
    return { mensaje: 'Sesión renovada.', ...(await this.emitirSesion(u, row.sesionId ?? undefined)) };
  }

  /**
   * Cierra la sesión: se revoca al instante (el gateway deja de aceptar el access token) y se invalida el refresh token.
   * Público: sirve aunque el access token ya haya vencido. Acepta el refresh token y/o el access token (Authorization).
   */
  async cerrarSesion(refreshToken?: string, bearer?: string) {
    let sid: string | null | undefined; let usuarioId: number | undefined;
    if (refreshToken) {
      const row = await this.tokens.findOne({ where: { tokenHash: sha256(refreshToken) } });
      if (row) { sid = row.sesionId; usuarioId = row.usuarioId; }
    }
    if (!sid && bearer) {
      try {
        const p: any = this.jwt.verify(bearer, { secret: cfg.jwtKey, issuer: cfg.issuer, audience: cfg.audience, ignoreExpiration: true });
        sid = p.sid; usuarioId = Number(p.sub);
      } catch { /* token ilegible: no hay nada que revocar */ }
    }
    if (sid) {
      await this.revocarSesion(sid);
      const u = usuarioId ? await this.users.findOne({ where: { usuarioId } }) : null;
      this.audit.registrar({ name: u?.nombre }, 'LOGOUT', 'El usuario cerró sesión.', undefined, undefined, usuarioId);
    }
    return { mensaje: 'Sesión cerrada correctamente.' };
  }

  private async revocarTodos(usuarioId: number) {
    const ahora = new Date();
    const ses = (await this.sesiones.find({ where: { usuarioId } })).filter((x) => !x.revocadaEn);
    ses.forEach((x) => (x.revocadaEn = ahora));
    if (ses.length) await this.sesiones.save(ses);
    const todos = (await this.tokens.find({ where: { usuarioId } })).filter((t) => !t.revocadoEn);
    todos.forEach((t) => (t.revocadoEn = ahora));
    if (todos.length) await this.tokens.save(todos);
  }

  async cambiarContrasenia(actor: Principal, d: { actual: string; nueva: string }) {
    if (!d?.actual) throw new BadRequestException({ mensaje: 'La contraseña actual es obligatoria.' });
    validarPolitica(d.nueva);
    const u = await this.users.findOne({ where: { usuarioId: Number(actor.sub) } });
    if (!u) throw new NotFoundException({ mensaje: 'Usuario no encontrado.' });
    if (!(await bcrypt.compare(d.actual, u.contraseniaHash))) throw new UnauthorizedException({ mensaje: 'La contraseña actual no es correcta.' });
    if (d.actual === d.nueva) throw new BadRequestException({ mensaje: 'La nueva contraseña debe ser distinta de la actual.' });
    await this.users.update(u.usuarioId, { contraseniaHash: await bcrypt.hash(d.nueva, 10) });
    await this.revocarTodos(u.usuarioId); // cierra las demás sesiones
    this.audit.registrar(actor, 'CAMBIAR_CONTRASENIA', 'El usuario cambió su contraseña.', 'Usuario', u.usuarioId);
    return { mensaje: 'Contraseña actualizada. Inicie sesión de nuevo.' };
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
