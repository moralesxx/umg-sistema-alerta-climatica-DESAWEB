import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BitacoraService, Bitacora } from '../../services/bitacora.service';
import { UsuarioService, UsuarioResumen } from '../../services/usuario.service';

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
  filtroEntidad = 'todas';   // RF-ADM-59: filtro por entidad

  // Se arma con las acciones y entidades que realmente existen en la bitacora
  accionesDisponibles: string[] = [];
  entidadesDisponibles: string[] = [];

  // id -> nombre, para mostrar el nombre del usuario en lugar de solo su número.
  private nombresUsuarios = new Map<number, string>();

  errorListado = '';
  errorFiltros = '';

  // Paginación
  paginaActual: number = 1;
  itemsPorPagina: number = 10;

  constructor(
    private bitacoraService: BitacoraService,
    private usuarioService: UsuarioService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarUsuarios();
    this.cargarBitacora();
  }

  // Solo el Administrador puede listar usuarios (igual que la bitácora). Si falla,
  // no pasa nada grave: la tabla sigue mostrando el id del usuario.
  private cargarUsuarios(): void {
    this.usuarioService.getUsuarios().subscribe({
      next: (usuarios: UsuarioResumen[]) => {
        this.nombresUsuarios = new Map(usuarios.map((u: UsuarioResumen): [number, string] => [u.usuarioId, u.nombre]));
        this.cdr.detectChanges();
      },
      error: () => { /* se queda con el id */ }
    });
  }

  cargarBitacora(): void {
    this.bitacoraService.obtenerBitacora().subscribe({
      next: (data: Bitacora[]) => {
        this.registros = data || [];
        this.accionesDisponibles = Array.from(
          new Set(this.registros.map(r => String(r.accion || '').toUpperCase()).filter(a => a))
        ).sort();
        this.entidadesDisponibles = Array.from(
          new Set(this.registros.map(r => String(r.entidad || '').trim()).filter(e => e))
        ).sort();
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

  // Fecha local en formato yyyy-MM-dd. Se evita toISOString() porque convierte a UTC:
  // un registro de las 19:00 en Guatemala (UTC-6) caía en el día siguiente.
  private fechaLocal(valor: string): string {
    const d = new Date(valor);
    const mes = String(d.getMonth() + 1).padStart(2, '0');
    const dia = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${mes}-${dia}`;
  }

  // Nombre del usuario según como lo entregue la API; si no viene, el id.
  nombreUsuario(item: Bitacora): string {
    return String(
      item.usuarioNombre ||
      item.usuario?.nombre ||
      this.nombresUsuarios.get(item.usuarioId) ||
      item.usuarioId ||
      ''
    );
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
        if (this.fechaLocal(item.fechaHora) < this.filtroFechaInicio) cumple = false;
      }

      if (this.filtroFechaFin) {
        if (this.fechaLocal(item.fechaHora) > this.filtroFechaFin) cumple = false;
      }

      if (this.filtroUsuario.trim()) {
        // Busca en el nombre y también en el id del usuario.
        const usuarioStr = `${this.nombreUsuario(item)} ${item.usuarioId}`.toLowerCase();
        if (!usuarioStr.includes(this.filtroUsuario.trim().toLowerCase())) cumple = false;
      }

      if (this.filtroAccion !== 'todas') {
        const accionStr = String(item.accion || '').toUpperCase();
        if (accionStr !== this.filtroAccion) cumple = false;
      }

      if (this.filtroEntidad !== 'todas') {
        if (String(item.entidad || '').trim() !== this.filtroEntidad) cumple = false;
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
    this.filtroEntidad = 'todas';
    this.errorFiltros = '';
    this.aplicarFiltros();
  }

  getAccionBadgeClass(accion: string): string {
    const a = String(accion || '').toUpperCase();
    if (a.includes('CREAR') || a.includes('INSERT')) return 'bg-success-subtle text-success border border-success-subtle';
    if (a.includes('ACTUALIZAR') || a.includes('UPDATE')) return 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
    if (a.includes('ELIMINAR') || a.includes('DELETE')) return 'bg-danger-subtle text-danger border border-danger-subtle';
    if (a.includes('ACTIVAR') || a.includes('ESTADO') || a.includes('ATEND') || a.includes('CERRAR')) return 'bg-primary-subtle text-primary-emphasis border border-primary-subtle';
    if (a.includes('LOGIN') || a.includes('LOGOUT')) return 'bg-info-subtle text-info-emphasis border border-info-subtle';
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