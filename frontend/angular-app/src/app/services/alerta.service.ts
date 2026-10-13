import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

export interface Alerta {
  alertaId?: number;
  sensorId: number;
  eventoId?: number | null;
  tipoAlerta: string;
  mensaje: string;
  nivelRiesgo: string;
  atendida: boolean;
  fechaEmision?: string;
}

// RF-ADM-34 / 38: alerta con todos los datos para mostrar (la arma el backend).
export interface AlertaDetalle {
  alertaId: number;
  fechaEmision: string;
  sensorId: number;
  sensorCodigo?: string | null;
  sensorNombre?: string | null;
  comunidadId?: number | null;
  comunidadNombre?: string | null;
  reglaAlertaId?: number | null;
  reglaNombre?: string | null;
  fenomeno?: string | null;
  nivelRiesgo: string;
  valorDetectado?: number | null;
  unidad?: string | null;
  umbral?: string | null;
  mensaje: string;
  estado: string;            // Activa | Atendida | Cerrada
  eventoId?: number | null;
  usuarioResponsableId?: number | null;
  usuarioResponsableNombre?: string | null;
  fechaGestion?: string | null;
}

// RF-ADM-39: filtros opcionales del listado de alertas.
export interface AlertaFiltros {
  fechaDesde?: string;
  fechaHasta?: string;
  comunidadId?: number;
  sensorId?: number;
  fenomeno?: string;
  nivel?: string;
  estado?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AlertaService {
  private apiUrl = '/api/alertas';

  // Subject para sincronizar el nivel de riesgo global entre componentes
  private nivelAlertaSource = new BehaviorSubject<string>('Verde');
  public nivelAlerta$ = this.nivelAlertaSource.asObservable();

  constructor(private http: HttpClient) { }

  setNivelAlerta(nivel: string): void {
    this.nivelAlertaSource.next(nivel);
  }

  getAlertas(): Observable<Alerta[]> {
    return this.http.get<Alerta[]>(this.apiUrl);
  }

  // Listado con filtros (RF-ADM-37 a 39).
  getAlertasFiltradas(filtros: AlertaFiltros = {}): Observable<AlertaDetalle[]> {
    let params = new HttpParams();
    if (filtros.fechaDesde) params = params.set('fechaDesde', filtros.fechaDesde);
    if (filtros.fechaHasta) params = params.set('fechaHasta', filtros.fechaHasta);
    if (filtros.comunidadId) params = params.set('comunidadId', filtros.comunidadId);
    if (filtros.sensorId) params = params.set('sensorId', filtros.sensorId);
    if (filtros.fenomeno) params = params.set('fenomeno', filtros.fenomeno);
    if (filtros.nivel) params = params.set('nivel', filtros.nivel);
    if (filtros.estado) params = params.set('estado', filtros.estado);
    return this.http.get<AlertaDetalle[]>(this.apiUrl, { params });
  }

  // RF-ADM-41 / 42: cambia el estado y registra al usuario responsable.
  atenderAlerta(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/atender`, {});
  }

  cerrarAlerta(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/cerrar`, {});
  }

  limpiarHistorial(): Observable<any> {
    return this.http.delete(this.apiUrl);
  }

  createAlerta(alerta: Alerta): Observable<Alerta> {
    return this.http.post<Alerta>(this.apiUrl, alerta);
  }

  // sensorId es opcional para no romper las llamadas existentes (antes era fijo = 1).
  simularAlerta(nivelRiesgo: string, mensajeDesc: string, sensorId: number = 1): Observable<Alerta> {
    // No se envía fechaEmision: la API usa su propia hora. Antes se mandaba en UTC
    // (toISOString) y quedaba desfasada respecto a bitácora y eventos.
    const nuevaAlerta: Alerta = {
      sensorId,
      eventoId: null,
      tipoAlerta: 'Climática',
      mensaje: mensajeDesc,
      nivelRiesgo: nivelRiesgo,
      atendida: false
    };
    return this.createAlerta(nuevaAlerta);
  }
}
