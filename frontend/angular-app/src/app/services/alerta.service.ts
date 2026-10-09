import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
