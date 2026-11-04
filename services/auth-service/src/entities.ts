import { Column, CreateDateColumn, Entity, Index, JoinColumn, ManyToOne, PrimaryColumn, PrimaryGeneratedColumn } from 'typeorm';

@Entity('roles')
export class Rol {
  @PrimaryGeneratedColumn({ name: 'rol_id' }) rolId: number;
  @Column({ name: 'nombre_rol', unique: true }) nombreRol: string;
  @Column({ type: 'varchar', nullable: true }) descripcion: string | null;
  /** Permisos finos que viajan en el JWT (claim `permissions`). */
  @Column('text', { array: true, default: '{}' }) permisos: string[];
}

@Entity('usuarios')
export class Usuario {
  @PrimaryGeneratedColumn({ name: 'usuario_id' }) usuarioId: number;
  @Column() nombre: string;
  @Index({ unique: true }) @Column() correo: string;
  /** Vacío cuando la cuenta solo existe por una identidad externa (Fase 4). */
  @Column({ name: 'contrasenia_hash', default: '' }) contraseniaHash: string;
  @Column({ name: 'rol_id' }) rolId: number;
  @ManyToOne(() => Rol, { eager: true }) @JoinColumn({ name: 'rol_id' }) rol: Rol;
  @Column({ default: true }) estado: boolean;
  @CreateDateColumn({ name: 'fecha_creacion', type: 'timestamptz' }) fechaCreacion: Date;
  @Column({ name: 'ultimo_acceso', type: 'timestamptz', nullable: true }) ultimoAcceso: Date | null;
  /** Protección contra fuerza bruta por cuenta (además del límite por IP del gateway). */
  @Column({ name: 'intentos_fallidos', default: 0 }) intentosFallidos: number;
  @Column({ name: 'bloqueado_hasta', type: 'timestamptz', nullable: true }) bloqueadoHasta: Date | null;
}

/** Sesión de usuario: su id viaja en el access token (claim `sid`). El gateway la consulta en cada petición, por eso el logout invalida el token al instante. */
@Entity('sesiones')
export class Sesion {
  @PrimaryColumn({ type: 'uuid' }) id: string;
  @Index() @Column({ name: 'usuario_id' }) usuarioId: number;
  @Column({ name: 'expira_en', type: 'timestamptz' }) expiraEn: Date;
  @Column({ name: 'revocada_en', type: 'timestamptz', nullable: true }) revocadaEn: Date | null;
  @CreateDateColumn({ name: 'creada_en', type: 'timestamptz' }) creadaEn: Date;
}

/** Refresh token opaco: solo se guarda su hash SHA-256. Rotación en cada uso y revocación en logout. */
@Entity('refresh_tokens')
export class RefreshToken {
  @PrimaryGeneratedColumn() id: number;
  @Index({ unique: true }) @Column({ name: 'token_hash' }) tokenHash: string;
  @Column({ name: 'usuario_id' }) usuarioId: number;
  @Column({ name: 'sesion_id', type: 'uuid', nullable: true }) sesionId: string | null;
  @Column({ name: 'expira_en', type: 'timestamptz' }) expiraEn: Date;
  @Column({ name: 'revocado_en', type: 'timestamptz', nullable: true }) revocadoEn: Date | null;
  @CreateDateColumn({ name: 'creado_en', type: 'timestamptz' }) creadoEn: Date;
}

/** Preparación OAuth 2.0 / OIDC: vincula un usuario local con (proveedor, subject externo). */
@Entity('identidades_externas')
@Index(['proveedor', 'subject'], { unique: true })
export class IdentidadExterna {
  @PrimaryGeneratedColumn() id: number;
  @Column() proveedor: string; // google | github | facebook | oidc:<issuer>
  @Column() subject: string;
  @Column({ name: 'usuario_id' }) usuarioId: number;
  @ManyToOne(() => Usuario, { onDelete: 'CASCADE' }) @JoinColumn({ name: 'usuario_id' }) usuario: Usuario;
  @CreateDateColumn({ name: 'fecha_vinculo', type: 'timestamptz' }) fechaVinculo: Date;
}
