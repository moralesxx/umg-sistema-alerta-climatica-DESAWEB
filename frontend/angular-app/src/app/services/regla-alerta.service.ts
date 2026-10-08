import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TipoSensor {
  tipoSensorId: number;
  nombreTipo: string;
  unidadMedida: string;
}

export interface ReglaAlerta {
  reglaAlertaId: number;
  nombre: string;
  tipoSensorId: number;
  valorMinimo: number | null;
  valorMaximo: number | null;
  nivelPeligro: string;
  tipoFenomeno: string;
  mensaje: string;
  estado: boolean;
  tipoSensor?: TipoSensor | null;
}

export interface CrearReglaAlerta {
  nombre: string;
  tipoSensorId: number;
  valorMinimo: number | null;
  valorMaximo: number | null;
  nivelPeligro: string;
  tipoFenomeno: string;
  mensaje: string;
  estado: boolean;
}

export interface ActualizarReglaAlerta extends CrearReglaAlerta {}

@Injectable({
  providedIn: 'root'
})
export class ReglaAlertaService {

  private apiUrl = 'http://localhost:5081/api/ReglaAlerta';

  constructor(private http: HttpClient) {}

  obtenerTodas(): Observable<ReglaAlerta[]> {
    return this.http.get<ReglaAlerta[]>(this.apiUrl);
  }

  obtenerPorId(id: number): Observable<ReglaAlerta> {
    return this.http.get<ReglaAlerta>(`${this.apiUrl}/${id}`);
  }

  crear(regla: CrearReglaAlerta): Observable<ReglaAlerta> {
    return this.http.post<ReglaAlerta>(this.apiUrl, regla);
  }

  actualizar(
    id: number,
    regla: ActualizarReglaAlerta
  ): Observable<ReglaAlerta> {
    return this.http.put<ReglaAlerta>(
      `${this.apiUrl}/${id}`,
      regla
    );
  }

  cambiarEstado(
    id: number,
    estado: boolean
  ): Observable<any> {
    return this.http.patch(
      `${this.apiUrl}/${id}/estado`,
      estado,
      {
        headers: {
          'Content-Type': 'application/json'
        }
      }
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}