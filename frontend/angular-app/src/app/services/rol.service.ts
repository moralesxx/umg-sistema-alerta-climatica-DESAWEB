import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Rol {
  rolId: number;
  nombreRol: string;
  descripcion?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RolService {
  private apiUrl = '/api/roles';

  constructor(private http: HttpClient) { }

  // Catálogo de roles (solo lo puede consultar el Administrador).
  getRoles(): Observable<Rol[]> {
    return this.http.get<Rol[]>(this.apiUrl);
  }
}
