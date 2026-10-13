import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { AlertaService } from '../../services/alerta.service';
import { ComunidadService, Comunidad } from '../../services/comunidad.service';
import { DashboardService, DashboardResumen, SerieLecturas } from '../../services/dashboard.service';

// Dashboard con datos reales de la API (RF-ADM-60 a 68). Se actualiza solo.
@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './overview.component.html',
  styleUrl: './overview.scss'
})
export class OverviewComponent implements OnInit, OnDestroy {
  datos: DashboardResumen | null = null;
  comunidades: Comunidad[] = [];

  // Filtros (RF-ADM-66)
  filtroComunidad = '';
  horas = 1;
  readonly opcionesHoras = [
    { valor: 1, texto: 'Última hora' },
    { valor: 6, texto: 'Últimas 6 horas' },
    { valor: 24, texto: 'Últimas 24 horas' }
  ];

  error = '';
  cargando = true;

  // Gráfica de evolución (RF-ADM-65)
  tipoSeleccionadoId: number | null = null;
  rutaGrafica = '';
  grafMin = 0;
  grafMax = 0;
  grafInicio = '';
  grafFin = '';
  readonly anchoGrafica = 500;
  readonly altoGrafica = 150;

  private refrescoSub?: Subscription;

  constructor(
    private dashboardService: DashboardService,
    private comunidadService: ComunidadService,
    private alertaService: AlertaService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.comunidadService.getComunidades().subscribe({
      next: (c) => { this.comunidades = c; this.cdr.detectChanges(); },
      error: () => {}
    });

    this.cargar();
    // RF-ADM-68: actualización dinámica cada 10 s.
    this.refrescoSub = interval(10000).subscribe(() => this.cargar());
  }

  ngOnDestroy(): void {
    this.refrescoSub?.unsubscribe();
  }

  cargar(): void {
    const comunidadId = this.filtroComunidad ? Number(this.filtroComunidad) : undefined;
    this.dashboardService.getResumen(comunidadId, this.horas).subscribe({
      next: (data) => {
        this.datos = data;
        this.error = '';
        this.cargando = false;

        // Mantiene sincronizado el nivel global con el botón "Simular alerta".
        this.alertaService.setNivelAlerta(data.nivelRiesgoGlobal);

        // Conserva el tipo elegido; si ya no existe, toma el primero.
        const existe = data.evolucion.some(s => s.tipoSensorId === this.tipoSeleccionadoId);
        if (!existe) {
          this.tipoSeleccionadoId = data.evolucion.length > 0 ? data.evolucion[0].tipoSensorId : null;
        }
        this.actualizarGrafica();
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'No se pudo cargar el dashboard.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  seleccionarTipo(id: number): void {
    this.tipoSeleccionadoId = id;
    this.actualizarGrafica();
  }

  get serieActual(): SerieLecturas | null {
    if (!this.datos || this.tipoSeleccionadoId === null) return null;
    return this.datos.evolucion.find(s => s.tipoSensorId === this.tipoSeleccionadoId) ?? null;
  }

  private actualizarGrafica(): void {
    const serie = this.serieActual;
    if (!serie || serie.puntos.length === 0) {
      this.rutaGrafica = '';
      this.grafInicio = '';
      this.grafFin = '';
      return;
    }

    const valores = serie.puntos.map(p => p.valor);
    let min = Math.min(...valores);
    let max = Math.max(...valores);
    if (min === max) { min -= 1; max += 1; }
    this.grafMin = min;
    this.grafMax = max;

    const tiempos = serie.puntos.map(p => new Date(p.fecha).getTime());
    const t0 = tiempos[0];
    const t1 = tiempos[tiempos.length - 1];
    const rango = Math.max(t1 - t0, 1);

    this.rutaGrafica = serie.puntos.map((p, i) => {
      const x = serie.puntos.length === 1 ? this.anchoGrafica / 2 : ((tiempos[i] - t0) / rango) * this.anchoGrafica;
      const y = this.altoGrafica - ((p.valor - min) / (max - min)) * this.altoGrafica;
      return `${i === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`;
    }).join(' ');

    const fmt = (t: number) => new Date(t).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' });
    this.grafInicio = fmt(t0);
    this.grafFin = fmt(t1);
  }

  porcentaje(total: number): number {
    const suma = this.datos?.alertasActivas ?? 0;
    return suma > 0 ? Math.round((total / suma) * 100) : 0;
  }

  getBadgeClass(texto: any): string {
    const n = String(texto || '').toLowerCase();
    if (n.includes('rojo') || n.includes('critica') || n.includes('crítica')) return 'bg-danger text-white';
    if (n.includes('naranja')) return 'bg-warning-orange text-white';
    if (n.includes('amarillo')) return 'bg-warning text-dark fw-bold';
    if (n.includes('verde')) return 'bg-success text-white';
    return 'bg-secondary text-white';
  }

  getBarraClass(nivel: string): string {
    switch (nivel.toLowerCase()) {
      case 'rojo': return 'bg-danger';
      case 'naranja': return 'bg-warning-orange';
      case 'amarillo': return 'bg-warning';
      default: return 'bg-success';
    }
  }
}
