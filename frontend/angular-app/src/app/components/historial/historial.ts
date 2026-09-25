import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventoService, Evento } from '../../services/evento.service';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './historial.component.html',
  styleUrl: './historial.scss'
})
export class HistorialComponent implements OnInit {
  eventos: Evento[] = [];

  // Propiedades de paginación
  paginaActual: number = 1;
  itemsPorPagina: number = 10;

  constructor(
    private eventoService: EventoService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarEventos();
  }

  cargarEventos(): void {
    this.eventoService.getEventos().subscribe({
      next: (data) => {
        this.eventos = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar eventos:', err)
    });
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
      this.cdr.detectChanges();
    }
  }
}
