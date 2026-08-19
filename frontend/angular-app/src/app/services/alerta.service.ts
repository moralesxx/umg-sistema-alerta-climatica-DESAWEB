import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Alerta {
  idAlerta: number;
  idSensor: number;
  idEvento: number;
  tipoAlerta: string;
  mensaje: string;
  nivel: string;
  atendida: boolean;
  fechaEmision: string;
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