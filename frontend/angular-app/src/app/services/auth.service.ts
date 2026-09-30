import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Usuario {
  usuarioId: number;
  nombre: string;
  correo: string;
  rolId: number;
  estado: boolean;
  rol: string;
}

export interface LoginResponse {
  mensaje: string;
  token: string;
  usuario: Usuario;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = 'http://localhost:5081/api/usuarios';

  constructor(private http: HttpClient) {}

  login(correo: string, contrasenia: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/login`,
      {
        correo,
        contrasenia
      }
    );
  }

  registrar(
    nombre: string,
    correo: string,
    contrasenia: string,
    rolId: number
  ): Observable<any> {
    return this.http.post(
      this.apiUrl,
      {
        nombre,
        correo,
        contrasenia,
        rolId
      }
    );
  }

  guardarSesion(respuesta: LoginResponse): void {
    localStorage.setItem('token', respuesta.token);
    localStorage.setItem(
      'usuario',
      JSON.stringify(respuesta.usuario)
    );
  }

  obtenerToken(): string | null {
    return localStorage.getItem('token');
  }

  obtenerUsuario(): Usuario | null {
    const usuario = localStorage.getItem('usuario');

    if (!usuario) {
      return null;
    }

    return JSON.parse(usuario);
  }

  cerrarSesion(): void {
    // RF-ADM-56: avisamos al backend ANTES de borrar el token,
    // para que quede registrado el LOGOUT en bitácora mientras
    // el token todavía es válido. Es "best effort": si falla
    // (por ejemplo sin conexión), igual cerramos sesión local.
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({
      error: () => {
        // Silenciado a propósito: cerrar sesión localmente no
        // debe depender de que el backend responda.
      }
    });

    localStorage.removeItem('token');
    localStorage.removeItem('usuario');
  }

  estaAutenticado(): boolean {
    return !!this.obtenerToken();
  }
}
