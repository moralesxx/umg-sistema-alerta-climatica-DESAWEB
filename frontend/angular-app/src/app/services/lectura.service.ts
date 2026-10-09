import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LecturaClimatica {
  lecturaId?: number;
  sensorId: number;
  valor: number;
  fechaHora?: string;
  unidad?: string;
  estadoSensor?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LecturaService {
  private apiUrl = '/api/lecturasclimaticas';

  constructor(private http: HttpClient) { }

  getLecturas(): Observable<LecturaClimatica[]> {
    return this.http.get<LecturaClimatica[]>(this.apiUrl);
  }

  getLecturasPorSensor(sensorId: number): Observable<LecturaClimatica[]> {
    return this.http.get<LecturaClimatica[]>(`${this.apiUrl}/sensor/${sensorId}`);
  }

  // RF-ADM-27: Consultar lecturas por comunidad
  getLecturasPorComunidad(comunidadId: number): Observable<LecturaClimatica[]> {
    return this.http.get<LecturaClimatica[]>(`${this.apiUrl}/comunidad/${comunidadId}`);
  }

  // RF-ADM-26: Filtrar por rango de fechas y/o sensor
  getLecturasPorFiltro(sensorId?: number, fechaInicio?: string, fechaFin?: string): Observable<LecturaClimatica[]> {
    let params = new HttpParams();
    if (sensorId) params = params.set('sensorId', sensorId.toString());
    if (fechaInicio) params = params.set('fechaInicio', fechaInicio);
    if (fechaFin) params = params.set('fechaFin', fechaFin);

    return this.http.get<LecturaClimatica[]>(`${this.apiUrl}/filtro`, { params });
  }
}