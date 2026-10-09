import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
  cantidadSensores?: number;   // RF-ADM-13 (calculado por el backend)
}

// RF-ADM-12: filtros opcionales para el listado de comunidades.
export interface ComunidadFiltros {
  busqueda?: string;
  estado?: boolean;
  municipio?: string;
  departamento?: string;
}

// Datos que se envían al crear o editar (RF-ADM-08 y RF-ADM-09).
// "estado" solo aplica al crear; para cambiarlo después se usa
// cambiarEstadoComunidad().
export interface ComunidadPayload {
  nombre: string;
  municipio: string;
  departamento: string;
  pais: string;
  latitud: number;
  longitud: number;
  descripcion: string;
  estado?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ComunidadService {
  private apiUrl = '/api/comunidades';

  constructor(private http: HttpClient) { }

  // Sin argumentos devuelve todas (lo usa el formulario de Sensores).
  // Con filtros, arma el query string y el backend hace el filtrado.
  getComunidades(filtros?: ComunidadFiltros): Observable<Comunidad[]> {
    let params = new HttpParams();

    if (filtros?.busqueda) {
      params = params.set('busqueda', filtros.busqueda);
    }
    if (filtros?.estado !== undefined && filtros?.estado !== null) {
      params = params.set('estado', filtros.estado);
    }
    if (filtros?.municipio) {
      params = params.set('municipio', filtros.municipio);
    }
    if (filtros?.departamento) {
      params = params.set('departamento', filtros.departamento);
    }

    return this.http.get<Comunidad[]>(this.apiUrl, { params });
  }

  getComunidad(id: number): Observable<Comunidad> {
    return this.http.get<Comunidad>(`${this.apiUrl}/${id}`);
  }

  crearComunidad(comunidad: ComunidadPayload): Observable<Comunidad> {
    return this.http.post<Comunidad>(this.apiUrl, comunidad);
  }

  actualizarComunidad(id: number, comunidad: ComunidadPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, comunidad);
  }

  // Activar o desactivar (PATCH). El cuerpo es solo true o false.
  cambiarEstadoComunidad(id: number, activa: boolean): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/estado`, activa);
  }
}
