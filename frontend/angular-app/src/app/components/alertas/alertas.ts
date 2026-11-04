import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AlertaService, Alerta } from '../../services/alerta.service';
import { AuthService } from '../../services/auth.service';
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

  // RF-ADM-07: el backend ya rechaza Simular Alerta y Limpiar con 403
  // para "Usuario de consulta". Esto solo oculta los botones en la UI,
  // mismo patrón que en Sensores.
  puedeAdministrar = false;

  showConfirmModal = false;
  confirmMessage = '';
  private confirmAction: (() => void) | null = null;

  showToast = false;
  toastMessage = '';

  constructor(
    private alertaService: AlertaService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    const usuario = this.authService.obtenerUsuario();
    this.puedeAdministrar = usuario?.rol === 'Administrador' || usuario?.rol === 'Operador';
  }

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

  // La bitácora de estas acciones (CREAR_ALERTA y ELIMINAR_ALERTAS) ahora la
  // registra el backend, así que ya no se envía desde aquí (evitaba filas duplicadas).

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