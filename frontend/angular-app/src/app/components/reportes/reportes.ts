import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReporteService, TipoReporte, ReporteMeta } from '../../services/reporte.service';
import { ComunidadService, Comunidad } from '../../services/comunidad.service';
import { SensorService, Sensor } from '../../services/sensor.service';
import { TipoSensorService, TipoSensor } from '../../services/tipo-sensor.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.component.html',
  styleUrl: './reportes.scss'
})
export class ReportesComponent implements OnInit, OnDestroy {
  tipos: TipoReporte[] = [];
  comunidades: Comunidad[] = [];
  sensores: Sensor[] = [];
  tiposSensor: TipoSensor[] = [];

  tipoSeleccionado = '';
  filtros: Record<string, any> = {};
  readonly niveles = ['Verde', 'Amarillo', 'Naranja', 'Rojo'];
  readonly fenomenos = ['Inundación', 'Sequía', 'Tormenta', 'Helada', 'Incendio forestal'];
  readonly estadosAlerta = ['Activa', 'Atendida', 'Cerrada'];

  historial: ReporteMeta[] = [];
  total = 0;
  pagina = 1;
  readonly tamano = 10;

  generando = false;
  error = '';
  mensaje = '';
  puedeDescargar = false;
  private timer: any;

  constructor(
    private reportes: ReporteService,
    private comunidadService: ComunidadService,
    private sensorService: SensorService,
    private tipoSensorService: TipoSensorService,
    private auth: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // RF-REP-10: la descarga es exclusiva de administradores (el backend lo valida también).
    this.puedeDescargar = this.auth.obtenerUsuario()?.rol === 'Administrador';
    this.reportes.tipos().subscribe({
      next: t => { this.tipos = t; this.tipoSeleccionado = t[0]?.type ?? ''; this.cdr.detectChanges(); },
      error: e => this.fallo(e, 'No se pudo cargar el catálogo de reportes.')
    });
    this.comunidadService.getComunidades().subscribe(c => { this.comunidades = c; this.cdr.detectChanges(); });
    this.sensorService.getSensores().subscribe(s => { this.sensores = s; this.cdr.detectChanges(); });
    this.tipoSensorService.getTiposSensor().subscribe(t => { this.tiposSensor = t; this.cdr.detectChanges(); });
    this.cargarHistorial();
    this.timer = setInterval(() => this.refrescarSiHayPendientes(), 3000);
  }

  ngOnDestroy(): void { clearInterval(this.timer); }

  get filtrosActivos(): string[] {
    return this.tipos.find(t => t.type === this.tipoSeleccionado)?.filters ?? [];
  }

  usa(f: string): boolean { return this.filtrosActivos.includes(f); }

  cambiarTipo(): void { this.filtros = {}; this.error = ''; this.mensaje = ''; }

  generar(): void {
    this.error = ''; this.mensaje = '';
    if (this.filtros['from'] && this.filtros['to'] && this.filtros['from'] > this.filtros['to']) {
      this.error = 'La fecha inicial no puede ser mayor que la final.';
      return;
    }
    const f: Record<string, any> = {};
    for (const k of this.filtrosActivos) {
      const v = this.filtros[k];
      if (v !== undefined && v !== null && v !== '') f[k] = v;
    }
    this.generando = true;
    this.reportes.generar(this.tipoSeleccionado, f).subscribe({
      next: m => {
        this.generando = false;
        this.mensaje = `Reporte ${m.reportId} en proceso. Aparecerá en el historial cuando termine.`;
        this.pagina = 1;
        this.cargarHistorial();
      },
      error: e => { this.generando = false; this.fallo(e, 'No se pudo solicitar el reporte.'); }
    });
  }

  cargarHistorial(): void {
    this.reportes.historial(this.pagina, this.tamano).subscribe({
      next: h => { this.historial = h.items; this.total = h.total; this.cdr.detectChanges(); },
      error: e => this.fallo(e, 'No se pudo cargar el historial.')
    });
  }

  private refrescarSiHayPendientes(): void {
    if (this.historial.some(r => r.status === 'PENDING' || r.status === 'PROCESSING')) this.cargarHistorial();
  }

  descargar(r: ReporteMeta): void {
    this.error = '';
    this.reportes.descargar(r.reportId).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `${r.reportId}-${r.fileName ?? 'reporte.pdf'}`;
        document.body.appendChild(a); a.click(); a.remove();
        URL.revokeObjectURL(url);
        this.cargarHistorial();
      },
      error: e => this.fallo(e, 'No se pudo descargar el reporte.')
    });
  }

  get paginas(): number { return Math.max(1, Math.ceil(this.total / this.tamano)); }
  irA(p: number): void { if (p >= 1 && p <= this.paginas) { this.pagina = p; this.cargarHistorial(); } }

  estadoTexto(s: string): string {
    return ({ PENDING: 'En cola', PROCESSING: 'Generando…', COMPLETED: 'Completado', FAILED: 'Con error' } as any)[s] ?? s;
  }

  resumenFiltros(f: Record<string, any>): string {
    const e = Object.entries(f ?? {});
    return e.length ? e.map(([k, v]) => `${k}: ${v}`).join(', ') : '—';
  }

  private fallo(e: any, def: string): void {
    this.error = e?.error?.mensaje ?? e?.error?.detail?.mensaje ?? def;
    this.cdr.detectChanges();
  }
}
