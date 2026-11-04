import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  EventoService,
  Evento,
  EventoFiltros,
  EventoEstadisticas,
  Conteo
} from '../../services/evento.service';
import { ComunidadService, Comunidad } from '../../services/comunidad.service';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './historial.component.html',
  styleUrl: './historial.scss'
})
export class HistorialComponent implements OnInit {
  eventos: Evento[] = [];
  comunidades: Comunidad[] = [];
  estadisticas: EventoEstadisticas | null = null;

  // Resumen breve (RF-ADM-48): niveles de más grave a menos grave y fenómenos.
  nivelesStats: Conteo[] = [];
  fenomenosStats: Conteo[] = [];

  // RF-ADM-47: fenómenos y niveles que se pueden filtrar.
  readonly fenomenos = ['Inundación', 'Sequía', 'Tormenta', 'Helada', 'Incendio forestal'];
  readonly niveles = ['Verde', 'Amarillo', 'Naranja', 'Rojo'];

  // RF-ADM-47: filtros (fecha, comunidad, fenómeno y nivel)
  filtroFechaInicio = '';
  filtroFechaFin = '';
  filtroComunidad: string = 'todas';  // 'todas' | id de la comunidad
  filtroFenomeno: string = 'todos';   // 'todos' | nombre del fenómeno
  filtroNivel: string = 'todos';      // 'todos' | nombre del nivel

  errorListado = '';
  errorFiltros = '';

  // Evento cuyo detalle está desplegado (se abre al hacer clic en la fila).
  eventoAbiertoId: number | null = null;

  // Propiedades de paginación
  paginaActual: number = 1;
  itemsPorPagina: number = 10;

  constructor(
    private eventoService: EventoService,
    private comunidadService: ComunidadService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarComunidades();
    this.cargarEventos();
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

  // Carga el listado (RF-ADM-46) y las estadísticas (RF-ADM-48) con los mismos filtros.
  cargarEventos(filtros?: EventoFiltros): void {
    this.eventoService.getEventos(filtros).subscribe({
      next: (data) => {
        this.eventos = data;
        this.errorListado = '';
        this.eventoAbiertoId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar eventos:', err);
        this.errorListado = 'No se pudo cargar el historial de eventos.';
        this.cdr.detectChanges();
      }
    });

    this.eventoService.getEstadisticas(filtros).subscribe({
      next: (data) => {
        this.estadisticas = data;

        const orden = ['Rojo', 'Naranja', 'Amarillo', 'Verde'];
        const posicion = (nombre: string) => {
          const i = orden.indexOf(nombre);
          return i < 0 ? 99 : i;
        };
        this.nivelesStats = [...data.porNivel].sort((a, b) => posicion(a.nombre) - posicion(b.nombre));
        this.fenomenosStats = data.porFenomeno.filter(c => c.nombre !== 'Sin dato');

        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar estadísticas:', err)
    });
  }

  private construirFiltros(): EventoFiltros {
    const filtros: EventoFiltros = {};

    if (this.filtroFechaInicio) filtros.fechaInicio = this.filtroFechaInicio;
    if (this.filtroFechaFin) filtros.fechaFin = this.filtroFechaFin;
    if (this.filtroComunidad !== 'todas') filtros.comunidadId = Number(this.filtroComunidad);
    if (this.filtroFenomeno !== 'todos') filtros.fenomeno = this.filtroFenomeno;
    if (this.filtroNivel !== 'todos') filtros.nivel = this.filtroNivel;

    return filtros;
  }

  // RF-ADM-47: aplica los filtros y vuelve a pedir la lista al backend.
  aplicarFiltros(): void {
    if (this.filtroFechaInicio && this.filtroFechaFin && this.filtroFechaInicio > this.filtroFechaFin) {
      this.errorFiltros = 'La fecha inicial no puede ser mayor que la fecha final.';
      return;
    }

    this.errorFiltros = '';
    this.paginaActual = 1;
    this.cargarEventos(this.construirFiltros());
  }

  limpiarFiltros(): void {
    this.filtroFechaInicio = '';
    this.filtroFechaFin = '';
    this.filtroComunidad = 'todas';
    this.filtroFenomeno = 'todos';
    this.filtroNivel = 'todos';
    this.errorFiltros = '';
    this.paginaActual = 1;
    this.cargarEventos();
  }

  // Botón "Actualizar": recarga manteniendo los filtros que estén puestos.
  actualizar(): void {
    this.cargarEventos(this.construirFiltros());
  }

  // Abre o cierra el detalle de un evento (valor, responsable, descripción completa).
  alternarDetalle(evento: Evento): void {
    const id = evento.eventoId ?? null;
    this.eventoAbiertoId = this.eventoAbiertoId === id ? null : id;
  }

  getBadgeClass(texto: any): string {
    const n = String(texto || '').toLowerCase();
    if (n.includes('rojo') || n.includes('critica') || n.includes('crítica')) return 'bg-danger text-white';
    if (n.includes('naranja')) return 'bg-warning-orange text-white';
    if (n.includes('amarillo')) return 'bg-warning text-dark fw-bold';
    if (n.includes('verde')) return 'bg-success text-white';
    return 'bg-secondary text-white';
  }

  getEstadoClass(estado: string | null | undefined): string {
    const e = String(estado || '').toLowerCase();
    if (e === 'activa') return 'bg-danger-subtle text-danger border border-danger-subtle';
    if (e === 'atendida') return 'bg-info-subtle text-info-emphasis border border-info-subtle';
    if (e === 'cerrada') return 'bg-secondary-subtle text-secondary-emphasis border border-secondary-subtle';
    return 'bg-light text-muted border';
  }

  // Métodos de control de paginación
  get totalPaginas(): number {
    return Math.ceil(this.eventos.length / this.itemsPorPagina) || 1;
  }

  get eventosPaginados(): Evento[] {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    return this.eventos.slice(inicio, inicio + this.itemsPorPagina);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
      this.eventoAbiertoId = null;
      this.cdr.detectChanges();
    }
  }

  // Calcula el porcentaje exacto para las gráficas de barra superiores
  calcularPorcentaje(parcial: number, total: number): number {
    if (!total || total === 0) return 0;
    return Math.round((parcial / total) * 100);
  }

  // Asigna el color correspondiente a la barra según el nivel de riesgo
  getBarraColorClass(nombreNivel: string): string {
    const n = String(nombreNivel || '').toLowerCase();
    if (n.includes('rojo') || n.includes('critica')) return 'bg-danger';
    if (n.includes('naranja')) return 'bg-warning-orange';
    if (n.includes('amarillo')) return 'bg-warning';
    if (n.includes('verde')) return 'bg-success';
    return 'bg-secondary';
  }
}