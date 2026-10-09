import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// Nota: AuthService ya exporta una interfaz "Usuario" (la sesión iniciada).
// Esta se llama UsuarioResumen para no confundirlas: es la fila del listado.
export interface UsuarioResumen {
  usuarioId: number;
  nombre: string;
  correo: string;
  rolId: number;
  estado: boolean;
  fechaCreacion: string;
  ultimoAcceso: string | null;   // null = nunca ha iniciado sesión (RF-ADM-55)
}

// RF-ADM-54: filtros opcionales del listado.
export interface UsuarioFiltros {
  texto?: string;     // busca en nombre o correo
  rolId?: number;
  estado?: boolean;
}

// RF-ADM-49: crear usuario como administrador (sí elige el rol).
export interface CrearUsuarioPayload {
  nombre: string;
  correo: string;
  contrasenia: string;
  rolId: number;
}

// RF-ADM-50 y RF-ADM-52: editar nombre, correo y rol. No incluye contraseña.
export interface ActualizarUsuarioPayload {
  nombre: string;
  correo: string;
  rolId: number;
}

@Injectable({
  providedIn: 'root'
})
export class UsuarioService {
  private apiUrl = '/api/usuarios';

  constructor(private http: HttpClient) { }

  getUsuarios(filtros?: UsuarioFiltros): Observable<UsuarioResumen[]> {
    let params = new HttpParams();

    if (filtros?.texto) {
      params = params.set('texto', filtros.texto);
    }
    if (filtros?.rolId !== undefined && filtros?.rolId !== null) {
      params = params.set('rolId', filtros.rolId);
    }
    if (filtros?.estado !== undefined && filtros?.estado !== null) {
      params = params.set('estado', filtros.estado);
    }

    return this.http.get<UsuarioResumen[]>(this.apiUrl, { params });
  }

  // POST api/usuarios/administracion (el POST api/usuarios a secas es el
  // registro público, que siempre asigna "Usuario de consulta").
  crearUsuario(usuario: CrearUsuarioPayload): Observable<UsuarioResumen> {
    return this.http.post<UsuarioResumen>(`${this.apiUrl}/administracion`, usuario);
  }

  actualizarUsuario(id: number, usuario: ActualizarUsuarioPayload): Observable<UsuarioResumen> {
    return this.http.put<UsuarioResumen>(`${this.apiUrl}/${id}`, usuario);
  }

  // Activar o desactivar (PATCH). El cuerpo es solo true o false.
  cambiarEstadoUsuario(id: number, activo: boolean): Observable<UsuarioResumen> {
    return this.http.patch<UsuarioResumen>(`${this.apiUrl}/${id}/estado`, activo);
  }
}
