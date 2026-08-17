import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Alerta {
  alertaId?: number;
  sensorId: number;
  nivelRiesgo: string; // Verde, Amarillo, Naranja, Rojo
  mensaje: string;
  fechaEmision?: string;
  estado: boolean;
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

  createAlerta(alerta: Alerta): Observable<Alerta> {
    return this.http.post<Alerta>(this.apiUrl, alerta);
  }
}
