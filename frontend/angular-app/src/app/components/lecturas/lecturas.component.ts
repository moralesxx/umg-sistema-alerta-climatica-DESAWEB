import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LecturaService, LecturaClimatica } from '../../services/lectura.service';

@Component({
  selector: 'app-lecturas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lecturas.component.html',
  styleUrl: './lecturas.component.scss'
})
export class LecturasComponent implements OnInit {
  lecturas: LecturaClimatica[] = [];

  // Filtros requeridos (RF-ADM-26 y RF-ADM-27)
  filtroSensorId?: number;
  filtroComunidadId?: number;
  fechaInicio?: string;
  fechaFin?: string;

  // Paginación
  paginaActual: number = 1;
  itemsPorPagina: number = 10;

  constructor(
    private lecturaService: LecturaService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarLecturas();
  }

  cargarLecturas(): void {
    this.lecturaService.getLecturas().subscribe({
      next: (data) => {
        this.lecturas = data;
        this.paginaActual = 1;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar lecturas:', err)
    });
  }

  buscarPorFiltros(): void {
    if (this.fechaInicio && this.fechaFin && this.fechaInicio > this.fechaFin) {
      alert('La fecha inicial no puede ser mayor que la fecha final.');
      return;
    }

    // Sensor, comunidad y fechas se combinan en una sola consulta (RF-ADM-26 y RF-ADM-27)
    this.lecturaService.getLecturasPorFiltro(
      this.filtroSensorId, this.fechaInicio, this.fechaFin, this.filtroComunidadId
    ).subscribe({
      next: (data) => {
        this.lecturas = data;
        this.paginaActual = 1;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al aplicar filtros:', err)
    });
  }

  limpiarFiltros(): void {
    this.filtroSensorId = undefined;
    this.filtroComunidadId = undefined;
    this.fechaInicio = undefined;
    this.fechaFin = undefined;
    this.cargarLecturas();
  }

  // Métodos de control de paginación
  get totalPaginas(): number {
    return Math.ceil(this.lecturas.length / this.itemsPorPagina) || 1;
  }

  get lecturasPaginadas(): LecturaClimatica[] {
    const inicio = (this.paginaActual - 1) * this.itemsPorPagina;
    return this.lecturas.slice(inicio, inicio + this.itemsPorPagina);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
      this.cdr.detectChanges();
    }
  }
}
