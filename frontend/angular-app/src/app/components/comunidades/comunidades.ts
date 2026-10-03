import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ComunidadService,
  Comunidad,
  ComunidadFiltros,
  ComunidadPayload
} from '../../services/comunidad.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-comunidades',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './comunidades.component.html',
  styleUrl: './comunidades.scss'
})
export class ComunidadesComponent implements OnInit {
  comunidades: Comunidad[] = [];

  // RF-ADM-07: crear, editar y activar/desactivar comunidades es solo del
  // Administrador. El backend ya responde 403 a los demás roles; esto solo
  // oculta los botones. La seguridad real vive en el backend.
  esAdministrador = false;

  // RF-ADM-12: búsqueda y filtros
  filtroBusqueda = '';
  filtroEstado: string = 'todos'; // 'todos' | 'true' | 'false'
  filtroMunicipio = '';
  filtroDepartamento = '';

  // Un solo formulario para crear y editar. Si comunidadEnEdicionId es null
  // el modal está en modo "crear".
  showFormModal = false;
  comunidadEnEdicionId: number | null = null;
  formNombre = '';
  formMunicipio = '';
  formDepartamento = '';
  formPais = '';
  formLatitud: number | null = null;
  formLongitud: number | null = null;
  formDescripcion = '';
  formEstado = true;
  errorFormulario = '';
  guardando = false;

  showConfirmModal = false;
  confirmMessage = '';
  private confirmAction: (() => void) | null = null;

  showToast = false;
  toastMessage = '';

  constructor(
    private comunidadService: ComunidadService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    const usuario = this.authService.obtenerUsuario();
    this.esAdministrador = usuario?.rol === 'Administrador';
  }

  ngOnInit(): void {
    this.cargarComunidades();
  }

  get modoEdicion(): boolean {
    return this.comunidadEnEdicionId !== null;
  }

  cargarComunidades(filtros?: ComunidadFiltros): void {
    this.comunidadService.getComunidades(filtros).subscribe({
      next: (data) => {
        this.comunidades = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar comunidades:', err)
    });
  }

  // RF-ADM-12: aplica los filtros y vuelve a pedir la lista al backend.
  aplicarFiltros(): void {
    const filtros: ComunidadFiltros = {};

    if (this.filtroBusqueda.trim()) filtros.busqueda = this.filtroBusqueda.trim();
    if (this.filtroEstado === 'true') filtros.estado = true;
    if (this.filtroEstado === 'false') filtros.estado = false;
    if (this.filtroMunicipio.trim()) filtros.municipio = this.filtroMunicipio.trim();
    if (this.filtroDepartamento.trim()) filtros.departamento = this.filtroDepartamento.trim();

    this.cargarComunidades(filtros);
  }

  limpiarFiltros(): void {
    this.filtroBusqueda = '';
    this.filtroEstado = 'todos';
    this.filtroMunicipio = '';
    this.filtroDepartamento = '';
    this.cargarComunidades();
  }

  // ---------- Crear (RF-ADM-08) ----------

  onAgregarComunidad(): void {
    this.comunidadEnEdicionId = null;
    this.formNombre = '';
    this.formMunicipio = '';
    this.formDepartamento = '';
    this.formPais = '';
    this.formLatitud = null;
    this.formLongitud = null;
    this.formDescripcion = '';
    this.formEstado = true;
    this.errorFormulario = '';
    this.showFormModal = true;
  }

  // ---------- Editar (RF-ADM-09) ----------

  onEditarComunidad(comunidad: Comunidad): void {
    this.comunidadEnEdicionId = comunidad.comunidadId;
    this.formNombre = comunidad.nombre;
    this.formMunicipio = comunidad.municipio;
    this.formDepartamento = comunidad.departamento;
    this.formPais = comunidad.pais;
    this.formLatitud = comunidad.latitud ?? null;
    this.formLongitud = comunidad.longitud ?? null;
    this.formDescripcion = comunidad.descripcion ?? '';
    this.formEstado = comunidad.estado;
    this.errorFormulario = '';
    this.showFormModal = true;
  }

  cancelarFormulario(): void {
    this.showFormModal = false;
    this.comunidadEnEdicionId = null;
    this.errorFormulario = '';
  }

  // Validación previa en el cliente, para dar una respuesta inmediata. El
  // backend repite estas mismas reglas (ComunidadService.Validar).
  private validarFormulario(): string {
    if (!this.formNombre.trim()) return 'El nombre es obligatorio.';
    if (!this.formMunicipio.trim()) return 'El municipio es obligatorio.';
    if (!this.formDepartamento.trim()) return 'El departamento es obligatorio.';
    if (!this.formPais.trim()) return 'El país es obligatorio.';
    if (this.formLatitud === null) return 'La latitud es obligatoria.';
    if (this.formLongitud === null) return 'La longitud es obligatoria.';
    if (this.formLatitud < -90 || this.formLatitud > 90) return 'La latitud debe estar entre -90 y 90.';
    if (this.formLongitud < -180 || this.formLongitud > 180) return 'La longitud debe estar entre -180 y 180.';
    if (!this.formDescripcion.trim()) return 'La descripción es obligatoria.';
    return '';
  }

  guardarFormulario(): void {
    if (this.guardando) return;

    this.errorFormulario = this.validarFormulario();
    if (this.errorFormulario) return;

    const payload: ComunidadPayload = {
      nombre: this.formNombre.trim(),
      municipio: this.formMunicipio.trim(),
      departamento: this.formDepartamento.trim(),
      pais: this.formPais.trim(),
      latitud: this.formLatitud as number,
      longitud: this.formLongitud as number,
      descripcion: this.formDescripcion.trim()
    };

    this.guardando = true;

    if (this.comunidadEnEdicionId === null) {
      payload.estado = this.formEstado;

      this.comunidadService.crearComunidad(payload).subscribe({
        next: () => this.alGuardarCorrectamente('Comunidad creada correctamente.'),
        error: (err) => this.alFallarGuardado(err)
      });
    } else {
      this.comunidadService.actualizarComunidad(this.comunidadEnEdicionId, payload).subscribe({
        next: () => this.alGuardarCorrectamente('Comunidad actualizada correctamente.'),
        error: (err) => this.alFallarGuardado(err)
      });
    }
  }

  private alGuardarCorrectamente(mensaje: string): void {
    this.guardando = false;
    this.showFormModal = false;
    this.comunidadEnEdicionId = null;
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
      this.errorFormulario = err.error?.mensaje ?? 'Ocurrió un error al guardar la comunidad.';
    }

    this.cdr.detectChanges();
  }

  // ---------- Activar / desactivar (RF-ADM-10) ----------

  onToggleEstado(comunidad: Comunidad): void {
    const nuevoEstado = !comunidad.estado;
    const accion = nuevoEstado ? 'activar' : 'desactivar';
    const sensores = comunidad.cantidadSensores ?? 0;

    this.confirmMessage = `¿Desea ${accion} la comunidad "${comunidad.nombre}"?`;

    // Se avisa de forma explícita que desactivar la comunidad NO desactiva sus sensores.
    if (!nuevoEstado && sensores > 0) {
      this.confirmMessage += ` Tiene ${sensores} sensor(es) asociado(s); seguirán con su propio estado.`;
    }

    this.confirmAction = () => {
      this.comunidadService.cambiarEstadoComunidad(comunidad.comunidadId, nuevoEstado).subscribe({
        next: () => {
          comunidad.estado = nuevoEstado;
          this.cdr.detectChanges();
          this.mostrarToast(`Comunidad ${nuevoEstado ? 'activada' : 'desactivada'} correctamente.`);
        },
        error: (err) => console.error('Error al cambiar estado de la comunidad:', err)
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
    setTimeout(() => {
      this.showToast = false;
      this.cdr.detectChanges();
    }, 3000);
  }
}
