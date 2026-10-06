import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  UsuarioService,
  UsuarioResumen,
  UsuarioFiltros
} from '../../services/usuario.service';
import { RolService, Rol } from '../../services/rol.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.scss'
})
export class UsuariosComponent implements OnInit {
  usuarios: UsuarioResumen[] = [];
  roles: Rol[] = [];

  // Id del administrador que tiene la sesión abierta. Sirve para no ofrecerle
  // desactivarse a sí mismo ni cambiarse su propio rol (podría dejar el
  // sistema sin administradores). El backend también bloquea desactivarse.
  usuarioActualId: number | null = null;

  // RF-ADM-54: búsqueda y filtros
  filtroTexto = '';
  filtroRol: string = 'todos';     // 'todos' | id del rol
  filtroEstado: string = 'todos';  // 'todos' | 'true' | 'false'

  // Un solo formulario para crear y editar. Si usuarioEnEdicionId es null,
  // el modal está en modo "crear".
  showFormModal = false;
  usuarioEnEdicionId: number | null = null;
  formNombre = '';
  formCorreo = '';
  formContrasenia = '';
  formRolId: number | null = null;
  mostrarContrasenia = false;
  errorFormulario = '';
  guardando = false;

  showConfirmModal = false;
  confirmMessage = '';
  private confirmAction: (() => void) | null = null;

  errorListado = '';

  showToast = false;
  toastMessage = '';

  constructor(
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.usuarioActualId = this.authService.obtenerUsuario()?.usuarioId ?? null;
  }

  ngOnInit(): void {
    this.cargarRoles();
    this.cargarUsuarios();
  }

  get modoEdicion(): boolean {
    return this.usuarioEnEdicionId !== null;
  }

  get editandoseASiMismo(): boolean {
    return this.usuarioEnEdicionId !== null && this.usuarioEnEdicionId === this.usuarioActualId;
  }

  // El listado solo trae RolId; el nombre se resuelve con el catálogo de roles.
  nombreRol(rolId: number): string {
    return this.roles.find(r => r.rolId === rolId)?.nombreRol ?? `Rol #${rolId}`;
  }

  cargarRoles(): void {
    this.rolService.getRoles().subscribe({
      next: (data) => {
        this.roles = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar roles:', err)
    });
  }

  cargarUsuarios(filtros?: UsuarioFiltros): void {
    this.usuarioService.getUsuarios(filtros).subscribe({
      next: (data) => {
        this.usuarios = data;
        this.errorListado = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar usuarios:', err);
        this.errorListado = err.status === 403
          ? 'No tienes permisos para ver los usuarios.'
          : 'No se pudo cargar el listado de usuarios.';
        this.cdr.detectChanges();
      }
    });
  }

  // RF-ADM-54: aplica los filtros y vuelve a pedir la lista al backend.
  aplicarFiltros(): void {
    const filtros: UsuarioFiltros = {};

    if (this.filtroTexto.trim()) filtros.texto = this.filtroTexto.trim();
    if (this.filtroRol !== 'todos') filtros.rolId = Number(this.filtroRol);
    if (this.filtroEstado === 'true') filtros.estado = true;
    if (this.filtroEstado === 'false') filtros.estado = false;

    this.cargarUsuarios(filtros);
  }

  limpiarFiltros(): void {
    this.filtroTexto = '';
    this.filtroRol = 'todos';
    this.filtroEstado = 'todos';
    this.cargarUsuarios();
  }

  // ---------- Crear (RF-ADM-49) ----------

  onAgregarUsuario(): void {
    this.usuarioEnEdicionId = null;
    this.formNombre = '';
    this.formCorreo = '';
    this.formContrasenia = '';
    this.formRolId = null;
    this.mostrarContrasenia = false;
    this.errorFormulario = '';
    this.showFormModal = true;
  }

  // ---------- Editar (RF-ADM-50 y RF-ADM-52) ----------

  onEditarUsuario(usuario: UsuarioResumen): void {
    this.usuarioEnEdicionId = usuario.usuarioId;
    this.formNombre = usuario.nombre;
    this.formCorreo = usuario.correo;
    this.formContrasenia = '';
    this.formRolId = usuario.rolId;
    this.errorFormulario = '';
    this.showFormModal = true;
  }

  cancelarFormulario(): void {
    this.showFormModal = false;
    this.usuarioEnEdicionId = null;
    this.errorFormulario = '';
  }

  // Validación previa en el cliente, para dar una respuesta inmediata. El
  // backend repite estas reglas (UsuarioService).
  private validarFormulario(): string {
    if (!this.formNombre.trim()) return 'El nombre es obligatorio.';
    if (!this.formCorreo.trim()) return 'El correo es obligatorio.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.formCorreo.trim())) return 'El correo no tiene un formato válido.';
    if (!this.modoEdicion && (!this.formContrasenia || this.formContrasenia.length < 6)) {
      return 'La contraseña debe tener al menos 6 caracteres.';
    }
    if (this.formRolId === null) return 'Debes seleccionar un rol.';
    return '';
  }

  guardarFormulario(): void {
    if (this.guardando) return;

    this.errorFormulario = this.validarFormulario();
    if (this.errorFormulario) return;

    this.guardando = true;

    if (this.usuarioEnEdicionId === null) {
      this.usuarioService.crearUsuario({
        nombre: this.formNombre.trim(),
        correo: this.formCorreo.trim(),
        contrasenia: this.formContrasenia,
        rolId: this.formRolId as number
      }).subscribe({
        next: () => this.alGuardarCorrectamente('Usuario creado correctamente.'),
        error: (err) => this.alFallarGuardado(err)
      });
    } else {
      this.usuarioService.actualizarUsuario(this.usuarioEnEdicionId, {
        nombre: this.formNombre.trim(),
        correo: this.formCorreo.trim(),
        rolId: this.formRolId as number
      }).subscribe({
        next: () => this.alGuardarCorrectamente('Usuario actualizado correctamente.'),
        error: (err) => this.alFallarGuardado(err)
      });
    }
  }

  private alGuardarCorrectamente(mensaje: string): void {
    this.guardando = false;
    this.showFormModal = false;
    this.usuarioEnEdicionId = null;
    this.aplicarFiltros();
    this.mostrarToast(mensaje);
  }

  // Muestra dentro del modal el mensaje que mandó el backend (400, 403, 404, 409).
  private alFallarGuardado(err: any): void {
    this.guardando = false;

    if (err.status === 403) {
      this.errorFormulario = 'No tienes permisos para realizar esta acción.';
    } else if (err.status === 0) {
      this.errorFormulario = 'No se pudo conectar con el servidor.';
    } else {
      this.errorFormulario = err.error?.mensaje ?? 'Ocurrió un error al guardar el usuario.';
    }

    this.cdr.detectChanges();
  }

  // ---------- Activar / desactivar (RF-ADM-51) ----------

  onToggleEstado(usuario: UsuarioResumen): void {
    const nuevoEstado = !usuario.estado;
    const accion = nuevoEstado ? 'activar' : 'desactivar';

    this.confirmMessage = `¿Desea ${accion} al usuario "${usuario.nombre}" (${usuario.correo})?`;

    if (!nuevoEstado) {
      this.confirmMessage += ' No podrá iniciar sesión mientras esté desactivado.';
    }

    this.confirmAction = () => {
      this.usuarioService.cambiarEstadoUsuario(usuario.usuarioId, nuevoEstado).subscribe({
        next: () => {
          usuario.estado = nuevoEstado;
          this.cdr.detectChanges();
          this.mostrarToast(`Usuario ${nuevoEstado ? 'activado' : 'desactivado'} correctamente.`);
        },
        error: (err) => {
          const mensaje = err.error?.mensaje ?? 'No se pudo cambiar el estado del usuario.';
          this.mostrarToast(mensaje);
        }
      });
    };
    this.showConfirmModal = true;
  }

  ejecutarConfirmacion(): void {
    if (this.confirmAction) this.confirmAction();
    this.showConfirmModal = false;
    this.confirmAction = null;
  }

  cancelarConfirmacion(): void {
    this.showConfirmModal = false;
    this.confirmAction = null;
  }

  mostrarToast(mensaje: string): void {
    this.toastMessage = mensaje;
    this.showToast = true;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.showToast = false;
      this.cdr.detectChanges();
    }, 3000);
  }
}
