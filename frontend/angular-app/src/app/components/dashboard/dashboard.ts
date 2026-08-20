import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SensorService, Sensor } from '../../services/sensor.service';
import { AlertaService, Alerta } from '../../services/alerta.service';
import { EventoService, Evento } from '../../services/evento.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  temperatura: number = 24.5;
  humedad: number = 68;
  velocidadViento: number = 18.2;
  nivelLluvia: number = 5.4;
  nivelRio: number = 1.8;

  nivelAlertaGlobal: string = 'Verde';
  alertasRecientes: Alerta[] = [];
  sensores: Sensor[] = [];
  eventos: Evento[] = [];

  // Estado de modales (reemplazan prompt/confirm/alert)
  showAddSensorModal = false;
  nuevoSensorNombre = '';
  nuevoSensorUbicacion = '';

  // Estado para modal de Editar Sensor
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
    private alertaService: AlertaService,
    private eventoService: EventoService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarSensores();
    this.cargarAlertas();
    this.cargarEventos();
    setInterval(() => this.simularLecturasTiempoReal(), 5000);
  }

  cargarSensores(): void {
    this.sensorService.getSensores().subscribe({
      next: (data) => { this.sensores = data; this.cdr.detectChanges(); },
      error: (err) => console.log('Error sensores:', err)
    });
  }

  cargarAlertas(): void {
    this.alertaService.getAlertas().subscribe({
      next: (data) => { this.alertasRecientes = data; this.cdr.detectChanges(); },
      error: (err) => console.log('Error alertas:', err)
    });
  }

  cargarEventos(): void {
    this.eventoService.getEventos().subscribe({
      next: (data) => { this.eventos = data; this.cdr.detectChanges(); },
      error: (err) => console.log('Error eventos:', err)
    });
  }

  onSimularAlerta(): void {
    const nivelActual = this.nivelAlertaGlobal;
    let mensajePrueba = '';

    // Generar descripción profesional y variada según el nivel de riesgo y los 5 fenómenos de la rúbrica
    switch (nivelActual.toLowerCase()) {
      case 'rojo':
        mensajePrueba = `Emergencia crítica: Desbordamiento inminente en el cauce principal del río detectado por sensores de caudal. Evacuación requerida.`;
        break;
      case 'naranja':
        mensajePrueba = `Alerta alta: Condiciones extremas de temperatura y sequedad con riesgo inminente de propagación de incendio forestal.`;
        break;
      case 'amarillo':
        mensajePrueba = `Precaución: Déficit prolongado de precipitaciones afectando la humedad del suelo y niveles de reservas hídricas (Sequía).`;
        break;
      case 'verde':
      default:
        mensajePrueba = `Condición normal/estable: Descenso leve de temperatura registrado en zona rural sin riesgo de helada severa.`;
        break;
    }

    this.alertaService.simularAlerta(nivelActual, mensajePrueba).subscribe({
      next: () => {
        this.cargarAlertas();
        this.cargarEventos();
      },
      error: (err) => console.error('Error al simular:', err)
    });
  }

  // ==========================================
  // MODAL: Agregar sensor
  // ==========================================
  onAgregarSensor(): void {
    this.nuevoSensorNombre = '';
    this.nuevoSensorUbicacion = '';
    this.showAddSensorModal = true;
  }

  cancelarAgregarSensor(): void {
    this.showAddSensorModal = false;
  }

  confirmarAgregarSensor(): void {
    if (!this.nuevoSensorNombre || !this.nuevoSensorUbicacion) return;

    const nuevoSensor: Sensor = {
      sensorId: 0,
      nombreSensor: this.nuevoSensorNombre,
      ubicacion: this.nuevoSensorUbicacion,
      estado: true,
      tipoSensorId: 1
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

  // ==========================================
  // MODAL: Editar sensor (Requerimiento de la rúbrica)
  // ==========================================
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
    if (this.sensorEnEdicionId === null || !this.editSensorNombre || !this.editSensorUbicacion) return;

    const sensorActualizado: Sensor = {
      sensorId: this.sensorEnEdicionId,
      nombreSensor: this.editSensorNombre,
      ubicacion: this.editSensorUbicacion,
      estado: this.editSensorEstado,
      tipoSensorId: this.editSensorTipoId
    };

    this.sensorService.updateSensor(this.sensorEnEdicionId, sensorActualizado).subscribe({
      next: () => {
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
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cambiar estado:', err)
    });
  }

  // ==========================================
  // MODAL: Confirmación genérica
  // ==========================================
  onLimpiarHistorial(): void {
    this.confirmMessage = '¿Desea limpiar el historial de alertas?';
    this.confirmAction = () => {
      this.alertaService.limpiarHistorial().subscribe({
        next: () => {
          this.alertasRecientes = [];
          this.cdr.detectChanges();
          this.mostrarToast('Historial de alertas limpiado.');
        },
        error: (err) => console.error('Error limpiar:', err)
      });
    };
    this.showConfirmModal = true;
  }

  onReiniciarSistema(): void {
    this.confirmMessage = '¿Desea reiniciar el sistema general de monitoreo?';
    this.confirmAction = () => {
      this.sensorService.reiniciarSistema().subscribe({
        next: () => {
          this.cargarSensores();
          this.cargarAlertas();
          this.cargarEventos();
          this.mostrarToast('Sistema de monitoreo reiniciado con éxito.');
        },
        error: (err) => console.error('Error al reiniciar:', err)
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

  // ==========================================
  // TOAST
  // ==========================================
  mostrarToast(mensaje: string): void {
    this.toastMessage = mensaje;
    this.showToast = true;
    setTimeout(() => { this.showToast = false; this.cdr.detectChanges(); }, 3000);
  }

  simularLecturasTiempoReal(): void {
    this.temperatura = Number((20 + Math.random() * 15).toFixed(1));
    this.humedad = Math.floor(50 + Math.random() * 40);
    this.velocidadViento = Number((10 + Math.random() * 30).toFixed(1));
    this.nivelLluvia = Number((Math.random() * 20).toFixed(1));
    this.nivelRio = Number((1.2 + Math.random() * 2.5).toFixed(2));

    const orden = ['Verde', 'Amarillo', 'Naranja', 'Rojo'];

    const rand = Math.random();
    let nivelBase: string;
    if (rand < 0.25) {
      nivelBase = 'Verde';
    } else if (rand < 0.50) {
      nivelBase = 'Amarillo';
    } else if (rand < 0.75) {
      nivelBase = 'Naranja';
    } else {
      nivelBase = 'Rojo';
    }

    if (nivelBase === this.nivelAlertaGlobal) {
      const rand2 = Math.random();
      if (rand2 < 0.25) nivelBase = 'Verde';
      else if (rand2 < 0.50) nivelBase = 'Amarillo';
      else if (rand2 < 0.75) nivelBase = 'Naranja';
      else nivelBase = 'Rojo';
    }

    let nivelSensores = 'Verde';
    if (this.nivelRio > 3.2 || this.nivelLluvia > 15) {
      nivelSensores = 'Rojo';
    } else if (this.nivelRio > 2.7 || this.velocidadViento > 35) {
      nivelSensores = 'Naranja';
    } else if (this.temperatura > 32 || this.humedad < 52) {
      nivelSensores = 'Amarillo';
    }

    const indiceFinal = Math.max(orden.indexOf(nivelBase), orden.indexOf(nivelSensores));
    this.nivelAlertaGlobal = orden[indiceFinal];
  }

  getBadgeClass(texto: any): string {
    const n = String(texto || '').toLowerCase();
    if (n.includes('rojo') || n.includes('critica') || n.includes('crítica')) return 'bg-danger text-white';
    if (n.includes('naranja')) return 'bg-warning-orange text-white';
    if (n.includes('amarillo')) return 'bg-warning text-dark fw-bold';
    if (n.includes('verde')) return 'bg-success text-white';
    return 'bg-secondary text-white';
  }
}
