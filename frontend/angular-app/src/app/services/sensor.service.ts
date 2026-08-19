import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Sensor {
  idSensor: number;
  nombre: string;
  ubicacion: string;
  latitud?: number;
  longitud?: number;
  fechaInstalacion?: string;
  estado: boolean;
  codigo?: string;
  idTipoSensor?: number;
  tipoSensor?: any;
}

@Injectable({
  providedIn: 'root'
})
export class SensorService {
  private apiUrl = 'http://localhost:5081/api/sensores';

  constructor(private http: HttpClient) { }

  getSensores(): Observable<Sensor[]> {
    return this.http.get<Sensor[]>(this.apiUrl);
  }

  getSensor(id: number): Observable<Sensor> {
    return this.http.get<Sensor>(`${this.apiUrl}/${id}`);
  }

  createSensor(sensor: Sensor): Observable<Sensor> {
    return this.http.post<Sensor>(this.apiUrl, sensor);
  }

  updateSensor(id: number, sensor: Sensor): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, sensor);
  }
}