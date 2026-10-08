import {
  Component,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  ReglaAlertaService,
  ReglaAlerta,
  CrearReglaAlerta,
  ActualizarReglaAlerta
} from '../../services/regla-alerta.service';

import {
  TipoSensorService,
  TipoSensor
} from '../../services/tipo-sensor.service';

@Component({
  selector: 'app-reglas-alerta',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './reglas-alerta.component.html',
  styleUrl: './reglas-alerta.component.scss'
})
export class ReglasAlertaComponent implements OnInit {

  reglas: ReglaAlerta[] = [];
  tiposSensor: TipoSensor[] = [];

  cargando = false;
  guardando = false;

  mostrarFormulario = false;
  modoEdicion = false;

  errorMessage = '';

  showConfirmModal = false;
  confirmMessage = '';
  reglaAEliminar: ReglaAlerta | null = null;

  showToast = false;
  toastMessage = '';

  // ID de la regla que se está editando
  reglaEditandoId: number | null = null;

  nivelesPeligro = [
    'Verde',
    'Amarillo',
    'Naranja',
    'Rojo'
  ];

  regla: CrearReglaAlerta = this.reglaVacia();

  constructor(
    private reglaAlertaService: ReglaAlertaService,
    private tipoSensorService: TipoSensorService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarTiposSensor();
    this.cargarReglas();
  }

  private reglaVacia(): CrearReglaAlerta {
    return {
      nombre: '',
      tipoSensorId: 0,
      valorMinimo: null,
      valorMaximo: null,
      nivelPeligro: 'Verde',
      tipoFenomeno: '',
      mensaje: '',
      estado: true
    };
  }

  // ==========================================================
  // CARGAR TIPOS DE SENSOR
  // ==========================================================

  cargarTiposSensor(): void {
    this.tipoSensorService.getTiposSensor().subscribe({
      next: (tipos) => {
        this.tiposSensor = tipos;

        this.cdr.detectChanges();
      },

      error: (error) => {
        console.error(
          'Error al cargar tipos de sensor:',
          error
        );

        this.errorMessage =
          'No se pudieron cargar los tipos de sensor.';

        this.cdr.detectChanges();
      }
    });
  }

  // ==========================================================
  // CARGAR REGLAS
  // ==========================================================

  cargarReglas(): void {
    this.cargando = true;
    this.errorMessage = '';

    this.cdr.detectChanges();

    this.reglaAlertaService.obtenerTodas().subscribe({
      next: (reglas) => {
        this.reglas = reglas;
        this.cargando = false;

        this.cdr.detectChanges();
      },

      error: (error) => {
        console.error(
          'Error al cargar reglas:',
          error
        );

        this.errorMessage =
          error?.error?.mensaje ||
          'No se pudieron cargar las reglas de alerta.';

        this.cargando = false;

        this.cdr.detectChanges();
      }
    });
  }

  // ==========================================================
  // NUEVA REGLA
  // ==========================================================

  nuevaRegla(): void {
    this.modoEdicion = false;
    this.reglaEditandoId = null;

    this.mostrarFormulario = true;
    this.errorMessage = '';

    this.regla = this.reglaVacia();

    this.cdr.detectChanges();
  }

  // ==========================================================
  // EDITAR REGLA
  // ==========================================================

  editarRegla(regla: ReglaAlerta): void {
    this.modoEdicion = true;

    this.reglaEditandoId =
      regla.reglaAlertaId;

    this.mostrarFormulario = true;
    this.errorMessage = '';

    this.regla = {
      nombre: regla.nombre,
      tipoSensorId: regla.tipoSensorId,
      valorMinimo: regla.valorMinimo,
      valorMaximo: regla.valorMaximo,
      nivelPeligro: regla.nivelPeligro,
      tipoFenomeno: regla.tipoFenomeno,
      mensaje: regla.mensaje,
      estado: regla.estado
    };

    this.cdr.detectChanges();
  }

  // ==========================================================
  // CANCELAR FORMULARIO
  // ==========================================================

  cancelarFormulario(): void {
    this.mostrarFormulario = false;
    this.modoEdicion = false;

    this.reglaEditandoId = null;

    this.errorMessage = '';

    this.regla = this.reglaVacia();

    this.cdr.detectChanges();
  }

  // ==========================================================
  // GUARDAR
  // ==========================================================

  guardarRegla(): void {
    this.errorMessage = '';

    if (!this.validarFormulario()) {
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.cdr.detectChanges();

    if (this.modoEdicion) {
      this.actualizarRegla();
    } else {
      this.crearRegla();
    }
  }

  // ==========================================================
  // CREAR
  // ==========================================================

  private crearRegla(): void {
    this.reglaAlertaService
      .crear(this.regla)
      .subscribe({

        next: (reglaCreada) => {

          console.log(
            'Regla creada:',
            reglaCreada
          );

          this.guardando = false;

          this.mostrarFormulario = false;
          this.modoEdicion = false;
          this.reglaEditandoId = null;

          this.regla = this.reglaVacia();

          this.mostrarToast(
            'Regla creada correctamente.'
          );

          this.cdr.detectChanges();

          this.cargarReglas();
        },

        error: (error) => {

          console.error(
            'Error al crear regla:',
            error
          );

          this.guardando = false;

          this.errorMessage =
            error?.error?.mensaje ||
            'No se pudo crear la regla de alerta.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==========================================================
  // ACTUALIZAR
  // ==========================================================

  private actualizarRegla(): void {

    if (this.reglaEditandoId === null) {

      this.guardando = false;

      this.errorMessage =
        'No se pudo identificar la regla a editar.';

      this.cdr.detectChanges();

      return;
    }

    const datos: ActualizarReglaAlerta = {
      ...this.regla
    };

    this.reglaAlertaService
      .actualizar(
        this.reglaEditandoId,
        datos
      )
      .subscribe({

        next: (reglaActualizada) => {

          console.log(
            'Regla actualizada:',
            reglaActualizada
          );

          this.guardando = false;

          this.mostrarFormulario = false;
          this.modoEdicion = false;
          this.reglaEditandoId = null;

          this.regla = this.reglaVacia();

          this.mostrarToast(
            'Regla actualizada correctamente.'
          );

          this.cdr.detectChanges();

          this.cargarReglas();
        },

        error: (error) => {

          console.error(
            'Error al actualizar regla:',
            error
          );

          this.guardando = false;

          this.errorMessage =
            error?.error?.mensaje ||
            'No se pudo actualizar la regla de alerta.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==========================================================
  // CAMBIAR ESTADO
  // ==========================================================

  cambiarEstado(regla: ReglaAlerta): void {

    const nuevoEstado = !regla.estado;

    this.reglaAlertaService
      .cambiarEstado(
        regla.reglaAlertaId,
        nuevoEstado
      )
      .subscribe({

        next: () => {

          regla.estado = nuevoEstado;

          this.mostrarToast(
            nuevoEstado
              ? 'Regla activada correctamente.'
              : 'Regla desactivada correctamente.'
          );

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'Error al cambiar estado:',
            error
          );

          this.errorMessage =
            error?.error?.mensaje ||
            'No se pudo cambiar el estado de la regla.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==========================================================
  // CONFIRMAR ELIMINACIÓN
  // ==========================================================

  confirmarEliminar(
    regla: ReglaAlerta
  ): void {

    this.reglaAEliminar = regla;

    this.confirmMessage =
      `¿Está seguro de eliminar la regla "${regla.nombre}"?`;

    this.showConfirmModal = true;

    this.cdr.detectChanges();
  }

  // ==========================================================
  // CANCELAR ELIMINACIÓN
  // ==========================================================

  cancelarEliminacion(): void {

    this.showConfirmModal = false;

    this.reglaAEliminar = null;

    this.confirmMessage = '';

    this.cdr.detectChanges();
  }

  // ==========================================================
  // EJECUTAR ELIMINACIÓN
  // ==========================================================

  ejecutarEliminacion(): void {

    if (!this.reglaAEliminar) {
      return;
    }

    const id =
      this.reglaAEliminar.reglaAlertaId;

    this.reglaAlertaService
      .eliminar(id)
      .subscribe({

        next: () => {

          this.showConfirmModal = false;

          this.reglaAEliminar = null;

          this.confirmMessage = '';

          this.mostrarToast(
            'Regla eliminada correctamente.'
          );

          this.cdr.detectChanges();

          this.cargarReglas();
        },

        error: (error) => {

          console.error(
            'Error al eliminar regla:',
            error
          );

          this.showConfirmModal = false;

          this.reglaAEliminar = null;

          this.confirmMessage = '';

          this.errorMessage =
            error?.error?.mensaje ||
            'No se pudo eliminar la regla.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==========================================================
  // OBTENER CONDICIÓN
  // ==========================================================

  obtenerCondicion(
    regla: ReglaAlerta
  ): string {

    if (
      regla.valorMinimo !== null &&
      regla.valorMaximo !== null
    ) {
      return `${regla.valorMinimo} - ${regla.valorMaximo}`;
    }

    if (regla.valorMinimo !== null) {
      return `≥ ${regla.valorMinimo}`;
    }

    if (regla.valorMaximo !== null) {
      return `≤ ${regla.valorMaximo}`;
    }

    return 'Sin condición';
  }

  // ==========================================================
  // CLASE DEL BADGE
  // ==========================================================

  getBadgeClass(
    nivel: string
  ): string {

    switch (nivel) {

      case 'Verde':
        return 'bg-success';

      case 'Amarillo':
        return 'bg-warning text-dark';

      case 'Naranja':
        return 'bg-warning-orange';

      case 'Rojo':
        return 'bg-danger';

      default:
        return 'bg-secondary';
    }
  }

  // ==========================================================
  // VALIDAR FORMULARIO
  // ==========================================================

  private validarFormulario(): boolean {

    if (!this.regla.nombre.trim()) {

      this.errorMessage =
        'Debe ingresar el nombre de la regla.';

      return false;
    }

    if (this.regla.tipoSensorId <= 0) {

      this.errorMessage =
        'Debe seleccionar un tipo de sensor.';

      return false;
    }

    if (
      this.regla.valorMinimo === null &&
      this.regla.valorMaximo === null
    ) {

      this.errorMessage =
        'Debe ingresar un valor mínimo o un valor máximo.';

      return false;
    }

    if (
      this.regla.valorMinimo !== null &&
      this.regla.valorMaximo !== null &&
      this.regla.valorMinimo >
      this.regla.valorMaximo
    ) {

      this.errorMessage =
        'El valor mínimo no puede ser mayor que el valor máximo.';

      return false;
    }

    if (!this.regla.nivelPeligro) {

      this.errorMessage =
        'Debe seleccionar el nivel de peligro.';

      return false;
    }

    if (!this.regla.tipoFenomeno.trim()) {

      this.errorMessage =
        'Debe ingresar el tipo de fenómeno.';

      return false;
    }

    if (!this.regla.mensaje.trim()) {

      this.errorMessage =
        'Debe ingresar el mensaje de alerta.';

      return false;
    }

    return true;
  }

  // ==========================================================
  // TOAST
  // ==========================================================

  private mostrarToast(
    mensaje: string
  ): void {

    this.toastMessage = mensaje;

    this.showToast = true;

    this.cdr.detectChanges();

    setTimeout(() => {

      this.showToast = false;

      this.cdr.detectChanges();

    }, 3000);
  }
}