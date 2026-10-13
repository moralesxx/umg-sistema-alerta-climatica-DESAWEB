import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { AlertaService, AlertaDetalle, AlertaFiltros } from '../../services/alerta.service';
import { AuthService } from '../../services/auth.service';
import { ComunidadService, Comunidad } from '../../services/comunidad.service';
import { SensorService, Sensor } from '../../services/sensor.service';

// Gestión de alertas (RF-ADM-37 a 43): listado, filtros, detalle y estados.
@Component({
  selector: 'app-alertas-gestion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './alertas-gestion.component.html',
  styleUrl: './alertas-gestion.scss'
})
export class AlertasGestionComponent implements OnInit, OnDestroy {
  alertas: AlertaDetalle[] = [];
  comunidades: Comunidad[] = [];
  sensores: Sensor[] = [];

  readonly fenomenos = ['Inundación', 'Sequía', 'Tormenta', 'Helada', 'Incendio forestal'];
  readonly niveles = ['Verde', 'Amarillo', 'Naranja', 'Rojo'];
  readonly estados = ['Activa', 'Atendida', 'Cerrada'];

  // Filtros (RF-ADM-39)
  filtroDesde = '';
  filtroHasta = '';
  filtroComunidad = '';
  filtroSensor = '';
  filtroFenomeno = '';
  filtroNivel = '';
  filtroEstado = '';

  cargando = false;
  error = '';
  alertaAbiertaId: number | null = null;
  procesandoId: number | null = null;

  // Solo Administrador y Operador gestionan; el backend igual lo valida (403).
  puedeGestionar = false;

  showToast = false;
  toastMessage = '';

  private refrescoSub?: Subscription;

  constructor(
    private alertaService: AlertaService,
    private authService: AuthService,
    private comunidadService: ComunidadService,
    private sensorService: SensorService,
    private cdr: ChangeDetectorRef
  ) {
    const usuario = this.authService.obtenerUsuario();
    this.puedeGestionar = usuario?.rol === 'Administrador' || usuario?.rol === 'Operador';
  }

  ngOnInit(): void {
    this.comunidadService.getComunidades().subscribe({
      next: (c) => { this.comunidades = c; this.cdr.detectChanges(); },
      error: () => {}
    });
    this.sensorService.getSensores().subscribe({
      next: (s) => { this.sensores = s; this.cdr.detectChanges(); },
      error: () => {}
    });

    this.cargar();
    // Las alertas las genera el motor solo: se refresca cada 15 s.
    this.refrescoSub = interval(15000).subscribe(() => this.cargar(false));
  }

  ngOnDestroy(): void {
    this.refrescoSub?.unsubscribe();
  }

  private armarFiltros(): AlertaFiltros {
    return {
      fechaDesde: this.filtroDesde || undefined,
      fechaHasta: this.filtroHasta || undefined,
      comunidadId: this.filtroComunidad ? Number(this.filtroComunidad) : undefined,
      sensorId: this.filtroSensor ? Number(this.filtroSensor) : undefined,
      fenomeno: this.filtroFenomeno || undefined,
      nivel: this.filtroNivel || undefined,
      estado: this.filtroEstado || undefined
    };
  }

  cargar(mostrarCarga = true): void {
    if (mostrarCarga) this.cargando = true;
    this.alertaService.getAlertasFiltradas(this.armarFiltros()).subscribe({
      next: (data) => {
        this.alertas = data;
        this.error = '';
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'No se pudieron cargar las alertas.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  limpiarFiltros(): void {
    this.filtroDesde = '';
    this.filtroHasta = '';
    this.filtroComunidad = '';
    this.filtroSensor = '';
    this.filtroFenomeno = '';
    this.filtroNivel = '';
    this.filtroEstado = '';
    this.cargar();
  }

  get activas(): number {
    return this.alertas.filter(a => a.estado === 'Activa').length;
  }

  alternarDetalle(a: AlertaDetalle): void {
    this.alertaAbiertaId = this.alertaAbiertaId === a.alertaId ? null : a.alertaId;
  }

  atender(a: AlertaDetalle, ev: Event): void {
    ev.stopPropagation();
    this.ejecutar(a, this.alertaService.atenderAlerta(a.alertaId), 'Alerta marcada como atendida.');
  }

  cerrar(a: AlertaDetalle, ev: Event): void {
    ev.stopPropagation();
    this.ejecutar(a, this.alertaService.cerrarAlerta(a.alertaId), 'Alerta cerrada.');
  }

  private ejecutar(a: AlertaDetalle, peticion: any, mensajeOk: string): void {
    this.procesandoId = a.alertaId;
    peticion.subscribe({
      next: () => {
        this.procesandoId = null;
        this.mostrarToast(mensajeOk);
        this.cargar(false);
      },
      error: (err: any) => {
        this.procesandoId = null;
        this.mostrarToast(err?.error?.mensaje || 'No se pudo actualizar la alerta.');
        this.cargar(false);
      }
    });
  }

  mostrarToast(mensaje: string): void {
    this.toastMessage = mensaje;
    this.showToast = true;
    setTimeout(() => {
      this.showToast = false;
      this.cdr.detectChanges();
    }, 3000);
  }

  nivelClase(nivel: string): string {
    switch ((nivel || '').toLowerCase()) {
      case 'rojo': return 'bg-danger text-white';
      case 'naranja': return 'bg-warning-orange text-white';
      case 'amarillo': return 'bg-warning text-dark';
      case 'verde': return 'bg-success text-white';
      default: return 'bg-secondary text-white';
    }
  }

  estadoClase(estado: string): string {
    switch (estado) {
      case 'Activa': return 'bg-danger-subtle text-danger';
      case 'Atendida': return 'bg-primary-subtle text-primary';
      default: return 'bg-secondary-subtle text-secondary';
    }
  }
}
