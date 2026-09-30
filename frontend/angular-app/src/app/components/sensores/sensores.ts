import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SensorService, Sensor, SensorFiltros } from '../../services/sensor.service';
import { TipoSensorService, TipoSensor } from '../../services/tipo-sensor.service';
import { ComunidadService, Comunidad } from '../../services/comunidad.service';
import { BitacoraService } from '../../services/bitacora.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sensores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sensores.component.html',
  styleUrl: './sensores.scss'
})
export class SensoresComponent implements OnInit {
  sensores: Sensor[] = [];
  tiposSensor: TipoSensor[] = [];
  comunidades: Comunidad[] = [];

  // RF-ADM-07: el backend ya rechaza estas acciones con 403 para
  // "Usuario de consulta". Esto solo oculta los botones en la UI
  // para que no vea controles que de todos modos le van a fallar;
  // la seguridad real sigue viviendo en el backend.
  puedeAdministrar = false;

  // RF-ADM-20: filtros
  filtroComunidadId: number | null = null;
  filtroTipoSensorId: number | null = null;
  filtroEstado: string = 'todos'; // 'todos' | 'true' | 'false'
  filtroCodigo: string = '';

  showAddSensorModal = false;
  nuevoSensorNombre = '';
  nuevoSensorUbicacion = '';
  nuevoSensorCodigo = '';
  nuevoSensorTipoId: number | null = null;
  nuevoSensorComunidadId: number | null = null;
  nuevoSensorFechaInstalacion = '';
  nuevoSensorDescripcion = '';

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

  showConfirmModal = false;
  confirmMessage = '';
  private confirmAction: (() => void) | null = null;

  showToast = false;
  toastMessage = '';

  constructor(
    private sensorService: SensorService,
    private tipoSensorService: TipoSensorService,
    private comunidadService: ComunidadService,
    private bitacoraService: BitacoraService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    const usuario = this.authService.obtenerUsuario();
    this.puedeAdministrar = usuario?.rol === 'Administrador' || usuario?.rol === 'Operador';
  }

  ngOnInit(): void {
    this.cargarSensores();
    this.cargarTiposSensor();
    this.cargarComunidades();
  }

  private registrarAccion(accion: string, detalle: string): void {
    this.bitacoraService.registrarAccion(accion, detalle).subscribe();
  }

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

  // Resuelve el nombre del tipo de sensor a partir de su Id, para mostrarlo
  // en la columna "TIPO" de la tabla sin tener que tocar el backend.
  obtenerNombreTipo(tipoSensorId?: number): string {
    if (!tipoSensorId) return '—';
    const tipo = this.tiposSensor.find(t => t.tipoSensorId === tipoSensorId);
    return tipo ? tipo.nombreTipo : '—';
  }

  // Igual que obtenerNombreTipo, pero para Comunidad.
  obtenerNombreComunidad(comunidadId?: number): string {
    if (!comunidadId) return '—';
    const comunidad = this.comunidades.find(c => c.comunidadId === comunidadId);
    return comunidad ? comunidad.nombre : '—';
  }

  // RF-ADM-20: aplica los filtros seleccionados y vuelve a pedir la lista al backend.
  aplicarFiltros(): void {
    const filtros: SensorFiltros = {};

    if (this.filtroComunidadId) filtros.comunidadId = this.filtroComunidadId;
    if (this.filtroTipoSensorId) filtros.tipoSensorId = this.filtroTipoSensorId;
    if (this.filtroEstado === 'true') filtros.estado = true;
    if (this.filtroEstado === 'false') filtros.estado = false;
    if (this.filtroCodigo.trim()) filtros.codigo = this.filtroCodigo.trim();

    this.cargarSensores(filtros);
  }

  limpiarFiltros(): void {
    this.filtroComunidadId = null;
    this.filtroTipoSensorId = null;
    this.filtroEstado = 'todos';
    this.filtroCodigo = '';
    this.cargarSensores();
  }

  onAgregarSensor(): void {
    // Cierra el formulario de edición si estaba abierto, para no mostrar ambos a la vez
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
    ) return;

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
      // Si se deja vacío, el backend genera un código automático.
      codigo: codigo ? codigo : undefined
    };

    this.sensorService.createSensor(nuevoSensor).subscribe({
      next: () => {
        this.registrarAccion('AGREGAR_SENSOR', `El usuario agregó el sensor "${nombreSensor}" en la ubicación "${ubicacion}".`);
        this.cargarSensores();
        this.showAddSensorModal = false;
        this.mostrarToast('Sensor agregado correctamente.');
      },
      error: (err) => console.error('Error al crear sensor:', err)
    });
  }

  onEditarSensor(sensor: Sensor): void {
    // Cierra el formulario de agregar si estaba abierto, para no mostrar ambos a la vez
    this.showAddSensorModal = false;

    this.sensorEnEdicionId = sensor.sensorId;
    this.editSensorNombre = sensor.nombreSensor;
    this.editSensorUbicacion = sensor.ubicacion;
    this.editSensorEstado = sensor.estado;
    this.editSensorTipoId = sensor.tipoSensorId || 1;
    this.editSensorCodigo = sensor.codigo || '';
    this.editSensorComunidadId = sensor.comunidadId ?? null;
    // El input type="date" de HTML espera "yyyy-MM-dd"; si el backend manda
    // fecha+hora, se recorta a solo la parte de la fecha.
    this.editSensorFechaInstalacion = sensor.fechaInstalacion ? sensor.fechaInstalacion.substring(0, 10) : '';
    this.editSensorDescripcion = sensor.descripcion || '';
    this.showEditSensorModal = true;
  }

  cancelarEdicionSensor(): void {
    this.showEditSensorModal = false;
    this.sensorEnEdicionId = null;
  }

  confirmarEdicionSensor(): void {
    if (this.sensorEnEdicionId === null || !this.editSensorNombre.trim() || !this.editSensorUbicacion.trim()) return;
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
      fechaInstalacion: this.editSensorFechaInstalacion ? this.editSensorFechaInstalacion : undefined,
      descripcion: descripcion ? descripcion : undefined,
      // Si se deja vacío, el backend conserva el código que ya tenía.
      codigo: codigo ? codigo : undefined
    };

    this.sensorService.updateSensor(sensorId, sensorActualizado).subscribe({
      next: () => {
        this.registrarAccion('EDITAR_SENSOR', `El usuario modificó el sensor #${sensorId}. Nuevo nombre: "${nombreSensor}", ubicación: "${ubicacion}".`);
        this.cargarSensores();
        this.showEditSensorModal = false;
        this.sensorEnEdicionId = null;
        this.mostrarToast('Sensor actualizado correctamente.');
      },
      error: (err) => console.error('Error al actualizar sensor:', err)
    });
  }

  onToggleEstado(sensor: Sensor): void {
    const nuevoEstado = !sensor.estado;
    this.sensorService.cambiarEstadoSensor(sensor.sensorId, nuevoEstado).subscribe({
      next: () => {
        sensor.estado = nuevoEstado;
        this.registrarAccion('CAMBIAR_ESTADO_SENSOR', `El usuario cambió el estado del sensor #${sensor.sensorId} a ${nuevoEstado ? 'Activo' : 'Inactivo'}.`);
        this.cdr.detectChanges();
        this.mostrarToast(`Sensor ${nuevoEstado ? 'activado' : 'desactivado'} correctamente.`);
      },
      error: (err) => console.error('Error al cambiar estado del sensor:', err)
    });
  }

  onReiniciarSistema(): void {
    this.confirmMessage = '¿Desea reiniciar el sistema general de monitoreo?';
    this.confirmAction = () => {
      this.sensorService.reiniciarSistema().subscribe({
        next: () => {
          this.registrarAccion('REINICIAR_SISTEMA', 'El usuario ejecutó el reinicio general del sistema de monitoreo.');
          this.cargarSensores();
          this.mostrarToast('Sistema de monitoreo reiniciado con éxito.');
        },
        error: (err) => console.error('Error al reiniciar sistema:', err)
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
