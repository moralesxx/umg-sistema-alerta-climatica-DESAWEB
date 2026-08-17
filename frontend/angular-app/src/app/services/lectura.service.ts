import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LecturaClimatica {
  lecturaId?: number;
  sensorId: number;
  valor: number;
  fechaHora?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LecturaService {
  private apiUrl = 'http://localhost:5081/api/lecturasclimaticas';

  constructor(private http: HttpClient) { }

  getLecturas(): Observable<LecturaClimatica[]> {
    return this.http.get<LecturaClimatica[]>(this.apiUrl);
  }

  getLecturasPorSensor(sensorId: number): Observable<LecturaClimatica[]> {
    return this.http.get<LecturaClimatica[]>(`${this.apiUrl}/sensor/${sensorId}`);
  }
}
