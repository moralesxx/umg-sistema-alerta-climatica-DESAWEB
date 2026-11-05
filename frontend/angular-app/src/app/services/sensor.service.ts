
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Sensor {
  sensorId: number;
  nombreSensor: string;
  ubicacion: string;
  latitud?: number;
  longitud?: number;
  fechaInstalacion?: string;
  estado: boolean;
  codigo?: string;
  tipoSensorId?: number;
  tipoSensor?: any;
  comunidadId?: number;
  descripcion?: string;
  comunidad?: any;
}

// Filtros opcionales para el listado de sensores.
export interface SensorFiltros {
  comunidadId?: number;
  tipoSensorId?: number;
  estado?: boolean;
  codigo?: string;
}

// Una lectura recibida desde Arduino.
export interface LecturaArduino {
  codigo: string;
  valor: number;
}

// Respuesta del backend después de procesar las lecturas.
export interface RespuestaIngestaBrowser {
  aceptadas: number;
  rechazadas: Array<{
    codigo?: string;
    motivo: string;
  }>;
  fechaHora: string;
}

@Injectable({
  providedIn: 'root'
})
export class SensorService {
  private apiUrl = '/api/sensors';

  constructor(private http: HttpClient) { }

  // ==========================================
  // CONSULTA DE SENSORES
  // ==========================================

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

  // ==========================================
  // ADMINISTRACIÓN DE SENSORES
  // ==========================================

  createSensor(sensor: Sensor): Observable<Sensor> {
    return this.http.post<Sensor>(this.apiUrl, sensor);
  }

  updateSensor(id: number, sensor: Sensor): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, sensor);
  }

  cambiarEstadoSensor(id: number, activo: boolean): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/estado`, activo);
  }

  reiniciarSistema(): Observable<any> {
    return this.http.post(`${this.apiUrl}/reiniciar`, {});
  }

  // ==========================================
  // INGESTA DE LECTURAS DESDE ARDUINO
  // ==========================================

  /**
   * Envía las lecturas recibidas desde Arduino mediante Web Serial.
   *
   * El interceptor global de Angular agrega automáticamente
   * el JWT del usuario autenticado.
   *
   * Endpoint:
   * POST /api/sensors/ingest/browser
   */
  ingestBrowser(
    lecturas: LecturaArduino[]
  ): Observable<RespuestaIngestaBrowser> {
    return this.http.post<RespuestaIngestaBrowser>(
      `${this.apiUrl}/ingest/browser`,
      { lecturas }
    );
  }
}
