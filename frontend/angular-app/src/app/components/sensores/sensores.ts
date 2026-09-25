import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SensorService, Sensor } from '../../services/sensor.service';
import { BitacoraService } from '../../services/bitacora.service';

@Component({
  selector: 'app-sensores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sensores.component.html',
  styleUrl: './sensores.scss'
})
export class SensoresComponent implements OnInit {
  sensores: Sensor[] = [];
  showAddSensorModal = false;
  nuevoSensorNombre = '';
  nuevoSensorUbicacion = '';

  showEditSensorModal = false;
  sensorEnEdicionId: number | null = null;
  editSensorNombre = '';
  editSensorUbicacion = '';
  editSensorEstado = true;
  editSensorTipoId = 1;

  showConfirmModal = false;
  confirmMessage = '';
  private confirmAction: (() => void) | null = null;

  showToast = false;
  toastMessage = '';

  constructor(
    private sensorService: SensorService,
    private bitacoraService: BitacoraService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarSensores();
  }

  private registrarAccion(accion: string, detalle: string): void {
    this.bitacoraService.registrarAccion(accion, detalle).subscribe();
  }

  cargarSensores(): void {
    this.sensorService.getSensores().subscribe({
      next: (data) => {
        this.sensores = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar sensores:', err)
    });
  }

  onAgregarSensor(): void {
    this.nuevoSensorNombre = '';
    this.nuevoSensorUbicacion = '';
    this.showAddSensorModal = true;
  }

  cancelarAgregarSensor(): void {
    this.showAddSensorModal = false;
  }

  confirmarAgregarSensor(): void {
    if (!this.nuevoSensorNombre.trim() || !this.nuevoSensorUbicacion.trim()) return;
    const nombreSensor = this.nuevoSensorNombre.trim();
    const ubicacion = this.nuevoSensorUbicacion.trim();

    const nuevoSensor: Sensor = {
      sensorId: 0,
      nombreSensor,
      ubicacion,
      estado: true,
      tipoSensorId: 1
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
    this.sensorEnEdicionId = sensor.sensorId;
    this.editSensorNombre = sensor.nombreSensor;
    this.editSensorUbicacion = sensor.ubicacion;
    this.editSensorEstado = sensor.estado;
    this.editSensorTipoId = sensor.tipoSensorId || 1;
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

    const sensorActualizado: Sensor = {
      sensorId,
      nombreSensor,
      ubicacion,
      estado: this.editSensorEstado,
      tipoSensorId: this.editSensorTipoId
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
