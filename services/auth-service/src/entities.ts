import { Column, CreateDateColumn, Entity, Index, JoinColumn, ManyToOne, PrimaryGeneratedColumn } from 'typeorm';

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
