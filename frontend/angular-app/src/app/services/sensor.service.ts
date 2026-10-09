import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
  comunidadId?: number;   // NUEVO (RF-ADM-15)
  descripcion?: string;   // NUEVO (RF-ADM-15)
  comunidad?: any;
}

// RF-ADM-20: filtros opcionales para el listado de sensores.
export interface SensorFiltros {
  comunidadId?: number;
  tipoSensorId?: number;
  estado?: boolean;
  codigo?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SensorService {
  private apiUrl = '/api/sensores';

  constructor(private http: HttpClient) { }

  // Sin argumentos devuelve todos los sensores (comportamiento de siempre).
  // Con filtros, arma el query string y el backend hace el filtrado.
  getSensores(filtros?: SensorFiltros): Observable<Sensor[]> {
    let params = new HttpParams();

    if (filtros?.comunidadId) {
      params = params.set('comunidadId', filtros.comunidadId);
    }
    if (filtros?.tipoSensorId) {
      params = params.set('tipoSensorId', filtros.tipoSensorId);
    }
    if (filtros?.estado !== undefined && filtros?.estado !== null) {
      params = params.set('estado', filtros.estado);
    }
    if (filtros?.codigo) {
      params = params.set('codigo', filtros.codigo);
    }

    return this.http.get<Sensor[]>(this.apiUrl, { params });
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
  // MÉTODOS PARA LA ADMINISTRACIÓN
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
