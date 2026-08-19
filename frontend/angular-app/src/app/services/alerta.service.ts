import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Alerta {
  alertaId?: number;      // Opcional para cuando se crea una nueva
  sensorId: number;
  eventoId?: number | null;
  tipoAlerta: string;
  mensaje: string;
  nivel: string;
  atendida: boolean;
  fechaEmision?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AlertaService {
  private apiUrl = 'http://localhost:5081/api/alertas';

  constructor(private http: HttpClient) { }

  getAlertas(): Observable<Alerta[]> {
    return this.http.get<Alerta[]>(this.apiUrl);
  }

  limpiarHistorial(): Observable<any> {
    return this.http.delete(this.apiUrl);
  }

  createAlerta(alerta: Alerta): Observable<Alerta> {
    return this.http.post<Alerta>(this.apiUrl, alerta);
  }

  // Método específico para simular o disparar alertas desde el dashboard
  simularAlerta(nivelRiesgo: string, mensajeDesc: string): Observable<Alerta> {
    const nuevaAlerta: Alerta = {
      sensorId: 1, // Usando el sensor predeterminado 1
      eventoId: null,
      tipoAlerta: 'Climática',
      mensaje: mensajeDesc,
      nivel: nivelRiesgo,
      atendida: false,
      fechaEmision: new Date().toISOString()
    };
    return this.createAlerta(nuevaAlerta);
  }
}
