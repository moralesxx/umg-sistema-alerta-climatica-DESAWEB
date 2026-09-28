import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Comunidad {
  comunidadId: number;
  nombre: string;
  municipio: string;
  departamento: string;
  pais: string;
  latitud?: number;
  longitud?: number;
  descripcion?: string;
  estado: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ComunidadService {
  private apiUrl = 'http://localhost:5081/api/comunidades';

  constructor(private http: HttpClient) { }

  getComunidades(): Observable<Comunidad[]> {
    return this.http.get<Comunidad[]>(this.apiUrl);
  }
}
