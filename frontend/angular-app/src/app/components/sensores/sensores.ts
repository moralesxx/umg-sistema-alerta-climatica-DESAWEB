
import {
  Component,
  OnInit,
  OnDestroy,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import {
  SensorService,
  Sensor,
  SensorFiltros,
} from '../../services/sensor.service';

import { TipoSensorService, TipoSensor } from '../../services/tipo-sensor.service';
import { ComunidadService, Comunidad } from '../../services/comunidad.service';
import { AuthService } from '../../services/auth.service';
import {
  ArduinoSerialService,
  EstadoArduino
} from '../../services/arduino-serial.service';

// ==========================================================
// TIPOS PARA WEB SERIAL
// ==========================================================


@Component({
  selector: 'app-sensores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sensores.component.html',
  styleUrl: './sensores.scss'
})
export class SensoresComponent implements OnInit, OnDestroy {

  sensores: Sensor[] = [];
  tiposSensor: TipoSensor[] = [];
  comunidades: Comunidad[] = [];

  // ==========================================================
  // PERMISOS
  // ==========================================================

  puedeAdministrar = false;

  // ==========================================================
  // FILTROS
  // ==========================================================

  filtroComunidadId: number | null = null;
  filtroTipoSensorId: number | null = null;
  filtroEstado: string = 'todos';
  filtroCodigo: string = '';

  // ==========================================================
  // MODAL AGREGAR SENSOR
  // ==========================================================

  showAddSensorModal = false;

  nuevoSensorNombre = '';
  nuevoSensorUbicacion = '';
  nuevoSensorCodigo = '';
  nuevoSensorTipoId: number | null = null;
  nuevoSensorComunidadId: number | null = null;
  nuevoSensorFechaInstalacion = '';
  nuevoSensorDescripcion = '';

  // ==========================================================
  // MODAL EDITAR SENSOR
  // ==========================================================

  showEditSensorModal = false;

  sensorEnEdicionId: number | null = null;

  editSensorNombre = '';
  editSensorUbicacion = '';
  editSensorCodigo = '';
  editSensorEstado = true;
  editSensorTipoId = 1;
  editSensorComunidadId: number | null = null;
  editSensorFechaInstalacion = '';
  editSensorDescripcion = '';

  // ==========================================================
  // MODAL CONFIRMACIÓN
  // ==========================================================

  showConfirmModal = false;
  confirmMessage = '';

  private confirmAction: (() => void) | null = null;

  // ==========================================================
  // TOAST
  // ==========================================================

  showToast = false;
  toastMessage = '';

  // ==========================================================
  // CONEXIÓN ARDUINO - WEB SERIAL
  // ==========================================================

  webSerialDisponible = false;

  arduinoConectado = false;
  conectandoArduino = false;
  desconectandoArduino = false;
  enviandoLecturas = false;

  estadoConexionArduino = 'Desconectado';

  mensajeArduino = 'Conecta tu Arduino UNO mediante USB para comenzar.';

  lecturasAceptadas = 0;
  lecturasRechazadas = 0;

  ultimaLecturaArduino = '';

   private estadoArduinoSub: Subscription | null = null;

  // ==========================================================
  // CONSTRUCTOR
  // ==========================================================

  constructor(
  private sensorService: SensorService,
  private tipoSensorService: TipoSensorService,
  private comunidadService: ComunidadService,
  private authService: AuthService,
  private arduinoService: ArduinoSerialService,
  private cdr: ChangeDetectorRef
  ) {
    const usuario = this.authService.obtenerUsuario();

    this.puedeAdministrar =
      usuario?.rol === 'Administrador' ||
      usuario?.rol === 'Operador';
  }

  // ==========================================================
  // INICIALIZACIÓN
  // ==========================================================

  ngOnInit(): void {
  this.cargarSensores();
  this.cargarTiposSensor();
  this.cargarComunidades();

  this.verificarCompatibilidadSerial();

  this.estadoArduinoSub = this.arduinoService.estado$.subscribe(
    (estado: EstadoArduino) => {
      this.arduinoConectado = estado.arduinoConectado;
      this.conectandoArduino = estado.conectandoArduino;
      this.desconectandoArduino = estado.desconectandoArduino;
      this.enviandoLecturas = estado.enviandoLecturas;
      this.estadoConexionArduino = estado.estadoConexionArduino;
      this.mensajeArduino = estado.mensajeArduino;
      this.lecturasAceptadas = estado.lecturasAceptadas;
      this.lecturasRechazadas = estado.lecturasRechazadas;
      this.ultimaLecturaArduino = estado.ultimaLecturaArduino;

      this.cdr.detectChanges();
    }
  );
}

ngOnDestroy(): void {
  this.estadoArduinoSub?.unsubscribe();

  // No desconectar Arduino: la conexión pertenece al servicio global.
}

  private verificarCompatibilidadSerial(): void {
    this.webSerialDisponible =
      typeof navigator !== 'undefined' &&
      'serial' in navigator &&
      typeof window !== 'undefined' &&
      window.isSecureContext;

    if (!this.webSerialDisponible) {
      this.mensajeArduino =
        'Web Serial requiere Chrome o Edge y una conexión HTTPS o localhost.';
    }
  }

  // ==========================================================
  // CARGA DE INFORMACIÓN
  // ==========================================================

  cargarSensores(filtros?: SensorFiltros): void {
    this.sensorService.getSensores(filtros).subscribe({
      next: (data) => {
        this.sensores = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar sensores:', err)
    });
  }

  cargarTiposSensor(): void {
    this.tipoSensorService.getTiposSensor().subscribe({
      next: (data) => {
        this.tiposSensor = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar tipos de sensor:', err)
    });
  }

  cargarComunidades(): void {
    this.comunidadService.getComunidades().subscribe({
      next: (data) => {
        this.comunidades = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar comunidades:', err)
    });
  }

  // ==========================================================
  // NOMBRES DE TIPO Y COMUNIDAD
  // ==========================================================

  obtenerNombreTipo(tipoSensorId?: number): string {
    if (!tipoSensorId) return '—';

    const tipo = this.tiposSensor.find(
      t => t.tipoSensorId === tipoSensorId
    );

    return tipo ? tipo.nombreTipo : '—';
  }

  obtenerNombreComunidad(comunidadId?: number): string {
    if (!comunidadId) return '—';

    const comunidad = this.comunidades.find(
      c => c.comunidadId === comunidadId
    );

    return comunidad ? comunidad.nombre : '—';
  }

  // ==========================================================
  // FILTROS
  // ==========================================================

  aplicarFiltros(): void {
    const filtros: SensorFiltros = {};

    if (this.filtroComunidadId) {
      filtros.comunidadId = this.filtroComunidadId;
    }

    if (this.filtroTipoSensorId) {
      filtros.tipoSensorId = this.filtroTipoSensorId;
    }

    if (this.filtroEstado === 'true') {
      filtros.estado = true;
    }

    if (this.filtroEstado === 'false') {
      filtros.estado = false;
    }

    if (this.filtroCodigo.trim()) {
      filtros.codigo = this.filtroCodigo.trim();
    }

    this.cargarSensores(filtros);
  }

  limpiarFiltros(): void {
    this.filtroComunidadId = null;
    this.filtroTipoSensorId = null;
    this.filtroEstado = 'todos';
    this.filtroCodigo = '';

    this.cargarSensores();
  }

  // ==========================================================
  // AGREGAR SENSOR
  // ==========================================================

  onAgregarSensor(): void {
    this.showEditSensorModal = false;
    this.sensorEnEdicionId = null;

    this.nuevoSensorNombre = '';
    this.nuevoSensorUbicacion = '';
    this.nuevoSensorCodigo = '';
    this.nuevoSensorTipoId = null;
    this.nuevoSensorComunidadId = null;
    this.nuevoSensorFechaInstalacion = '';
    this.nuevoSensorDescripcion = '';

    this.showAddSensorModal = true;
  }

  cancelarAgregarSensor(): void {
    this.showAddSensorModal = false;
  }

  confirmarAgregarSensor(): void {
    if (
      !this.nuevoSensorNombre.trim() ||
      !this.nuevoSensorUbicacion.trim() ||
      !this.nuevoSensorTipoId ||
      !this.nuevoSensorFechaInstalacion ||
      !this.nuevoSensorDescripcion.trim()
    ) {
      return;
    }

    const nombreSensor = this.nuevoSensorNombre.trim();
    const ubicacion = this.nuevoSensorUbicacion.trim();
    const codigo = this.nuevoSensorCodigo.trim();
    const descripcion = this.nuevoSensorDescripcion.trim();

    const nuevoSensor: Sensor = {
      sensorId: 0,
      nombreSensor,
      ubicacion,
      estado: true,
      tipoSensorId: this.nuevoSensorTipoId,
      comunidadId: this.nuevoSensorComunidadId ?? undefined,
      fechaInstalacion: this.nuevoSensorFechaInstalacion,
      descripcion,
      codigo: codigo ? codigo : undefined
    };

    this.sensorService.createSensor(nuevoSensor).subscribe({
      next: () => {
        this.cargarSensores();
        this.showAddSensorModal = false;
        this.mostrarToast('Sensor agregado correctamente.');
      },
      error: (err) => console.error('Error al crear sensor:', err)
    });
  }

  // ==========================================================
  // EDITAR SENSOR
  // ==========================================================

  onEditarSensor(sensor: Sensor): void {
    this.showAddSensorModal = false;

    this.sensorEnEdicionId = sensor.sensorId;

    this.editSensorNombre = sensor.nombreSensor;
    this.editSensorUbicacion = sensor.ubicacion;
    this.editSensorEstado = sensor.estado;
    this.editSensorTipoId = sensor.tipoSensorId || 1;
    this.editSensorCodigo = sensor.codigo || '';
    this.editSensorComunidadId = sensor.comunidadId ?? null;

    this.editSensorFechaInstalacion =
      sensor.fechaInstalacion
        ? sensor.fechaInstalacion.substring(0, 10)
        : '';

    this.editSensorDescripcion = sensor.descripcion || '';

    this.showEditSensorModal = true;
  }

  cancelarEdicionSensor(): void {
    this.showEditSensorModal = false;
    this.sensorEnEdicionId = null;
  }

  confirmarEdicionSensor(): void {
    if (
      this.sensorEnEdicionId === null ||
      !this.editSensorNombre.trim() ||
      !this.editSensorUbicacion.trim()
    ) {
      return;
    }

    const sensorId = this.sensorEnEdicionId;
    const nombreSensor = this.editSensorNombre.trim();
    const ubicacion = this.editSensorUbicacion.trim();
    const codigo = this.editSensorCodigo.trim();
    const descripcion = this.editSensorDescripcion.trim();

    const sensorActualizado: Sensor = {
      sensorId,
      nombreSensor,
      ubicacion,
      estado: this.editSensorEstado,
      tipoSensorId: this.editSensorTipoId,
      comunidadId: this.editSensorComunidadId ?? undefined,
      fechaInstalacion: this.editSensorFechaInstalacion
        ? this.editSensorFechaInstalacion
        : undefined,
      descripcion: descripcion ? descripcion : undefined,
      codigo: codigo ? codigo : undefined
    };

    this.sensorService.updateSensor(sensorId, sensorActualizado).subscribe({
      next: () => {
        this.cargarSensores();
        this.showEditSensorModal = false;
        this.sensorEnEdicionId = null;

        this.mostrarToast('Sensor actualizado correctamente.');
      },
      error: (err) => console.error('Error al actualizar sensor:', err)
    });
  }

  // ==========================================================
  // ACTIVAR / DESACTIVAR SENSOR
  // ==========================================================

  onToggleEstado(sensor: Sensor): void {
    const nuevoEstado = !sensor.estado;

    this.sensorService.cambiarEstadoSensor(
      sensor.sensorId,
      nuevoEstado
    ).subscribe({
      next: () => {
        sensor.estado = nuevoEstado;

        this.cdr.detectChanges();

        this.mostrarToast(
          `Sensor ${nuevoEstado ? 'activado' : 'desactivado'} correctamente.`
        );
      },
      error: (err) => console.error('Error al cambiar estado del sensor:', err)
    });
  }

  // ==========================================================
  // REINICIAR SISTEMA
  // ==========================================================

  onReiniciarSistema(): void {
    this.confirmMessage =
      '¿Desea reiniciar el sistema general de monitoreo?';

    this.confirmAction = () => {
      this.sensorService.reiniciarSistema().subscribe({
        next: () => {
          this.cargarSensores();

          this.mostrarToast(
            'Sistema de monitoreo reiniciado con éxito.'
          );
        },
        error: (err) => console.error('Error al reiniciar sistema:', err)
      });
    };

    this.showConfirmModal = true;
  }

  ejecutarConfirmacion(): void {
    if (this.confirmAction) {
      this.confirmAction();
    }

    this.showConfirmModal = false;
    this.confirmAction = null;
  }

  cancelarConfirmacion(): void {
    this.showConfirmModal = false;
    this.confirmAction = null;
  }

    // ==========================================================
  // CONECTAR ARDUINO - SERVICIO GLOBAL
  // ==========================================================

  async conectarArduino(): Promise<void> {
    if (!this.puedeAdministrar) {
      this.mensajeArduino =
        'No tienes permisos para conectar dispositivos.';
      return;
    }

    if (!this.webSerialDisponible) {
      this.mensajeArduino =
        'Tu navegador no permite Web Serial o la página no utiliza HTTPS.';
      return;
    }

    if (this.arduinoConectado || this.conectandoArduino) {
      return;
    }

    await this.arduinoService.conectar();
  }

  // ==========================================================
  // DESCONECTAR ARDUINO - SERVICIO GLOBAL
  // ==========================================================

  async desconectarArduino(): Promise<void> {
    await this.arduinoService.desconectar();
  }

  // ==========================================================
  // NOTIFICACIONES
  // ==========================================================

  mostrarToast(mensaje: string): void {
    this.toastMessage = mensaje;
    this.showToast = true;

    setTimeout(() => {
      this.showToast = false;
      this.cdr.detectChanges();
    }, 3000);
  }
}
