import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// RF-ADM-45: datos de cada evento. Los campos opcionales vienen en null en
// los eventos creados antes de la Fase 2.
export interface Evento {
  eventoId?: number;
  tipoFenomeno: string;
  descripcion: string;
  fechaHora?: string;
  comunidadId?: number | null;
  comunidadNombre?: string | null;
  sensorId?: number | null;
  sensorNombre?: string | null;
  nivelRiesgo?: string | null;
  valor?: number | null;
  estado?: string | null;
  usuarioResponsableId?: number | null;
  usuarioResponsableNombre?: string | null;
}

// RF-ADM-47: filtros opcionales del historial.
// Las fechas van en formato yyyy-MM-dd (lo que entrega <input type="date">).
export interface EventoFiltros {
  fechaInicio?: string;
  fechaFin?: string;
  comunidadId?: number;
  fenomeno?: string;
  nivel?: string;
}

export interface Conteo {
  nombre: string;
  total: number;
}

// RF-ADM-48
export interface EventoEstadisticas {
  total: number;
  porFenomeno: Conteo[];
  porNivel: Conteo[];
  porEstado: Conteo[];
  porComunidad: Conteo[];
}

@Injectable({
  providedIn: 'root'
})
export class EventoService {
  private apiUrl = '/api/eventos';

  constructor(private http: HttpClient) { }

  // Sin argumentos devuelve todos. Con filtros, el backend hace el filtrado.
  getEventos(filtros?: EventoFiltros): Observable<Evento[]> {
    return this.http.get<Evento[]>(this.apiUrl, { params: this.armarParams(filtros) });
  }

  // RF-ADM-48: estadísticas con los mismos filtros del listado.
  getEstadisticas(filtros?: EventoFiltros): Observable<EventoEstadisticas> {
    return this.http.get<EventoEstadisticas>(`${this.apiUrl}/estadisticas`, {
      params: this.armarParams(filtros)
    });
  }

  crearEvento(evento: Evento): Observable<Evento> {
    return this.http.post<Evento>(this.apiUrl, evento);
  }

  private armarParams(filtros?: EventoFiltros): HttpParams {
    let params = new HttpParams();

    if (filtros?.fechaInicio) {
      params = params.set('fechaInicio', filtros.fechaInicio);
    }
    if (filtros?.fechaFin) {
      params = params.set('fechaFin', filtros.fechaFin);
    }
    if (filtros?.comunidadId !== undefined && filtros?.comunidadId !== null) {
      params = params.set('comunidadId', filtros.comunidadId);
    }
    if (filtros?.fenomeno) {
      params = params.set('fenomeno', filtros.fenomeno);
    }
    if (filtros?.nivel) {
      params = params.set('nivel', filtros.nivel);
    }

    return params;
  }
}