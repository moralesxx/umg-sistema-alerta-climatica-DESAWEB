import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// Respuesta de GET /api/dashboard/resumen (RF-ADM-60 a 68).
export interface ConteoNivel { nivel: string; total: number; }

export interface AlertaActivaResumen {
  alertaId: number;
  fechaEmision: string;
  nivelRiesgo: string;
  fenomeno?: string | null;
  sensorNombre?: string | null;
  comunidadNombre?: string | null;
  valorDetectado?: number | null;
  unidad?: string | null;
  mensaje: string;
}

export interface ValorActual {
  tipoSensorId: number;
  tipo: string;
  unidad: string;
  valor: number;
  sensores: number;
  fecha: string;
}

export interface PuntoLectura { fecha: string; valor: number; }

export interface SerieLecturas {
  tipoSensorId: number;
  tipo: string;
  unidad: string;
  puntos: PuntoLectura[];
}

export interface EventoResumen {
  eventoId: number;
  fechaHora: string;
  fenomeno: string;
  nivelRiesgo?: string | null;
  comunidadNombre?: string | null;
  sensorNombre?: string | null;
  valor?: number | null;
  estado?: string | null;
}

export interface DashboardResumen {
  totalComunidades: number;
  comunidadesActivas: number;
  sensoresActivos: number;
  sensoresInactivos: number;
  alertasActivas: number;
  nivelRiesgoGlobal: string;
  alertasPorNivel: ConteoNivel[];
  alertasActivasRecientes: AlertaActivaResumen[];
  valoresActuales: ValorActual[];
  evolucion: SerieLecturas[];
  eventosRecientes: EventoResumen[];
  generadoEn: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = '/api/dashboard';

  constructor(private http: HttpClient) { }

  getResumen(comunidadId?: number, horas: number = 1): Observable<DashboardResumen> {
    let params = new HttpParams().set('horas', horas);
    if (comunidadId) params = params.set('comunidadId', comunidadId);
    return this.http.get<DashboardResumen>(`${this.apiUrl}/resumen`, { params });
  }
}
