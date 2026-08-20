import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Sensor {
  sensorId: number;       // Coincide con SensorId de C# -> sensorId
  nombreSensor: string;   // Coincide con NombreSensor de C# -> nombreSensor
  ubicacion: string;
  latitud?: number;
  longitud?: number;
  fechaInstalacion?: string;
  estado: boolean;
  codigo?: string;
  tipoSensorId?: number;  // Coincide con TipoSensorId de C# -> tipoSensorId
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

  // ==========================================
  // NUEVOS MÉTODOS PARA LA ADMINISTRACIÓN
  // ==========================================

  // Permite activar o desactivar un sensor rápidamente (PATCH)
  cambiarEstadoSensor(id: number, activo: boolean): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/estado`, activo);
  }

  // Reinicia el sistema de monitoreo general (POST)
  reiniciarSistema(): Observable<any> {
    return this.http.post(`${this.apiUrl}/reiniciar`, {});
  }
}
