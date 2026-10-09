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

  private apiUrl = '/api/usuarios';

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

    // Si alguien manipuló el almacenamiento y el JSON quedó roto,
    // no se debe romper la aplicación: se trata como "sin sesión".
    try {
      return JSON.parse(usuario);
    } catch {
      return null;
    }
  }

  // RF-ADM-04: lee la expiración (claim "exp") del JWT. La firma NO se valida
  // aquí (eso lo hace la API); solo sirve para no mostrar el panel con un token vencido.
  private tokenExpirado(token: string): boolean {
    try {
      const payload = token.split('.')[1];
      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      const exp = JSON.parse(json).exp;

      return typeof exp === 'number' && Date.now() >= exp * 1000;
    } catch {
      // Token ilegible: se considera inválido.
      return true;
    }
  }

  cerrarSesion(): void {
    // RF-ADM-56: avisamos al backend ANTES de borrar el token,
    // para que quede registrado el LOGOUT en bitácora mientras
    // el token todavía es válido. Es "best effort": si falla
    // (por ejemplo sin conexión), igual cerramos sesión local.
    // El token se envía explícito (no depende de que el interceptor lo lea
    // antes de que lo borremos justo abajo).
    const token = this.obtenerToken();

    if (token) {
      this.http.post(
        `${this.apiUrl}/logout`,
        {},
        { headers: { Authorization: `Bearer ${token}` } }
      ).subscribe({
        error: () => {
          // Silenciado a propósito: cerrar sesión localmente no
          // debe depender de que el backend responda.
        }
      });
    }

    localStorage.removeItem('token');
    localStorage.removeItem('usuario');
  }

  // RF-ADM-02 y RF-ADM-04: con token vencido la sesión se limpia y se pide login de nuevo.
  estaAutenticado(): boolean {
    const token = this.obtenerToken();

    if (!token) {
      return false;
    }

    if (this.tokenExpirado(token)) {
      localStorage.removeItem('token');
      localStorage.removeItem('usuario');
      return false;
    }

    return true;
  }
}
