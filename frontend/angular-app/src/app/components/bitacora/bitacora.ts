import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BitacoraService, Bitacora } from '../../services/bitacora.service';

@Component({
  selector: 'app-bitacora',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bitacora.component.html',
  styleUrl: './bitacora.scss'
})
export class BitacoraComponent implements OnInit {
  registros: Bitacora[] = [];
  registrosFiltrados: Bitacora[] = [];

  // Filtros de búsqueda
  filtroFechaInicio = '';
  filtroFechaFin = '';
  filtroUsuario = '';
  filtroAccion = 'todas';

  readonly accionesDisponibles = ['CREAR', 'ACTUALIZAR', 'ELIMINAR', 'LOGIN', 'EXPORTAR'];

  errorListado = '';
  errorFiltros = '';

  // Paginación
  paginaActual: number = 1;
  itemsPorPagina: number = 10;

  constructor(
    private bitacoraService: BitacoraService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarBitacora();
  }

  cargarBitacora(): void {
    this.bitacoraService.obtenerBitacora().subscribe({
      next: (data: Bitacora[]) => {
        this.registros = data || [];
        this.aplicarFiltros();
        this.errorListado = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Error al cargar la bitácora de auditoría:', err);
        this.errorListado = 'No se pudo cargar el historial de auditoría.';
        this.cdr.detectChanges();
      }
    });
  }

  aplicarFiltros(): void {
    if (this.filtroFechaInicio && this.filtroFechaFin && this.filtroFechaInicio > this.filtroFechaFin) {
      this.errorFiltros = 'La fecha inicial no puede ser mayor que la fecha final.';
      return;
    }

    this.errorFiltros = '';
    this.paginaActual = 1;

    this.registrosFiltrados = this.registros.filter(item => {
      let cumple = true;

      if (this.filtroFechaInicio) {
        const fechaItem = new Date(item.fechaHora).toISOString().split('T')[0];
        if (fechaItem < this.filtroFechaInicio) cumple = false;
      }

      if (this.filtroFechaFin) {
        const fechaItem = new Date(item.fechaHora).toISOString().split('T')[0];
        if (fechaItem > this.filtroFechaFin) cumple = false;
      }

      if (this.filtroUsuario.trim()) {
        const usuarioStr = String((item as any).usuarioNombre || item.usuarioId || '').toLowerCase();
        if (!usuarioStr.includes(this.filtroUsuario.toLowerCase())) cumple = false;
      }

      if (this.filtroAccion !== 'todas') {
        const accionStr = String(item.accion || '').toUpperCase();
        if (accionStr !== this.filtroAccion) cumple = false;
      }

      return cumple;
    });

    this.cdr.detectChanges();
  }

  limpiarFiltros(): void {
    this.filtroFechaInicio = '';
    this.filtroFechaFin = '';
    this.filtroUsuario = '';
    this.filtroAccion = 'todas';
    this.errorFiltros = '';
    this.aplicarFiltros();
  }

  getAccionBadgeClass(accion: string): string {
    const a = String(accion || '').toUpperCase();
    if (a.includes('CREAR') || a.includes('INSERT')) return 'bg-success-subtle text-success border border-success-subtle';
    if (a.includes('ACTUALIZAR') || a.includes('UPDATE')) return 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
    if (a.includes('ELIMINAR') || a.includes('DELETE')) return 'bg-danger-subtle text-danger border border-danger-subtle';
    if (a.includes('LOGIN')) return 'bg-info-subtle text-info-emphasis border border-info-subtle';
    return 'bg-secondary-subtle text-secondary-emphasis border';
  }

  // Paginación
  get totalPaginas(): number {
    return Math.ceil(this.registrosFiltrados.length / this.itemsPorPagina) || 1;
  }

  get registrosPaginados(): Bitacora[] {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    return this.registrosFiltrados.slice(inicio, inicio + this.itemsPorPagina);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
      this.cdr.detectChanges();
    }
  }
}