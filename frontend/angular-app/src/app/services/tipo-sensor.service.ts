import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TipoSensor {
  tipoSensorId: number;
  nombreTipo: string;
  unidadMedida: string;
}

@Injectable({
  providedIn: 'root'
})
export class TipoSensorService {
  private apiUrl = 'http://localhost:5081/api/tipossensor';

  constructor(private http: HttpClient) { }

  getTiposSensor(): Observable<TipoSensor[]> {
    return this.http.get<TipoSensor[]>(this.apiUrl);
  }
}
