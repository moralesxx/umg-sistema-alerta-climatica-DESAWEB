import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AlertaService, Alerta } from '../../services/alerta.service';
import { BitacoraService } from '../../services/bitacora.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-alertas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alertas.component.html',
  styleUrl: './alertas.scss'
})
export class AlertasComponent implements OnInit, OnDestroy {
  alertasRecientes: Alerta[] = [];
  nivelAlertaGlobal: string = 'Verde';
  private alertaSub!: Subscription;

  showConfirmModal = false;
  confirmMessage = '';
  private confirmAction: (() => void) | null = null;

  showToast = false;
  toastMessage = '';

  constructor(
    private alertaService: AlertaService,
    private bitacoraService: BitacoraService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarAlertas();
    this.alertaSub = this.alertaService.nivelAlerta$.subscribe(nivel => {
      this.nivelAlertaGlobal = nivel;
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    if (this.alertaSub) {
      this.alertaSub.unsubscribe();
    }
  }

  private registrarAccion(accion: string, detalle: string): void {
    this.bitacoraService.registrarAccion(accion, detalle).subscribe();
  }

  cargarAlertas(): void {
    this.alertaService.getAlertas().subscribe({
      next: (data) => {
        this.alertasRecientes = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar alertas:', err)
    });
  }

  onSimularAlerta(): void {
    const nivelActual = this.nivelAlertaGlobal;
    let mensajePrueba = '';

    switch (nivelActual.toLowerCase()) {
      case 'rojo':
        mensajePrueba = 'Emergencia crítica: Desbordamiento inminente en el cauce principal del río detectado por sensores de caudal. Evacuación requerida.';
        break;
      case 'naranja':
        mensajePrueba = 'Alerta alta: Condiciones extremas de temperatura y sequedad con riesgo inminente de propagación de incendio forestal.';
        break;
      case 'amarillo':
        mensajePrueba = 'Precaución: Déficit prolongado de precipitaciones afectando la humedad del suelo y niveles de reservas hídricas (Sequía).';
        break;
      case 'verde':
      default:
        mensajePrueba = 'Condición normal/estable: Descenso leve de temperatura registrado en zona rural sin riesgo de helada severa.';
        break;
    }

    this.alertaService.simularAlerta(nivelActual, mensajePrueba).subscribe({
      next: () => {
        this.registrarAccion('SIMULAR_ALERTA', `El usuario simuló una alerta de nivel ${nivelActual}.`);
        this.cargarAlertas();
        this.mostrarToast('Alerta simulada correctamente.');
      },
      error: (err) => console.error('Error al simular alerta:', err)
    });
  }

  onLimpiarHistorial(): void {
    this.confirmMessage = '¿Desea limpiar el historial de alertas?';
    this.confirmAction = () => {
      this.alertaService.limpiarHistorial().subscribe({
        next: () => {
          this.alertasRecientes = [];
          this.registrarAccion('LIMPIAR_HISTORIAL_ALERTAS', 'El usuario limpió el historial de alertas del sistema.');
          this.cdr.detectChanges();
          this.mostrarToast('Historial de alertas limpiado.');
        },
        error: (err) => console.error('Error al limpiar historial:', err)
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

  getBadgeClass(texto: any): string {
    const n = String(texto || '').toLowerCase();
    if (n.includes('rojo') || n.includes('critica') || n.includes('crítica')) return 'bg-danger text-white';
    if (n.includes('naranja')) return 'bg-warning-orange text-white';
    if (n.includes('amarillo')) return 'bg-warning text-dark fw-bold';
    if (n.includes('verde')) return 'bg-success text-white';
    return 'bg-secondary text-white';
  }
}
