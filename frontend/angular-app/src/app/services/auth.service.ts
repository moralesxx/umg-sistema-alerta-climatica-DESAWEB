import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, map, shareReplay, tap } from 'rxjs';

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
  refreshToken?: string;
  expiresIn?: number;
  usuario: Usuario;
}

export type ProveedorOAuth = 'google' | 'github';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = '/api/auth';

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

  // ---------------------------------------------------------------------------------
  // Login con Google (OpenID Connect) y GitHub (OAuth 2.0) — Authorization Code + PKCE.
  // Todo el protocolo (secretos, state, nonce, validación) ocurre en el Auth Service; el
  // navegador solo navega a /start y, al volver, canjea un ticket de un solo uso.
  // ---------------------------------------------------------------------------------

  /** Proveedores que el servidor tiene configurados (para mostrar solo los botones que funcionan). */
  oauthProveedores(): Observable<ProveedorOAuth[]> {
    return this.http
      .get<{ providers: ProveedorOAuth[] }>(`${this.apiUrl}/oauth/providers`)
      .pipe(map((r) => r.providers ?? []));
  }

  /** Redirige (navegación completa, no XHR) al proveedor a través del Auth Service. */
  iniciarOAuth(proveedor: ProveedorOAuth): void {
    window.location.assign(`${this.apiUrl}/oauth/${proveedor}/start`);
  }

  /** Canjea el ticket de un solo uso (refresh token rotativo) que dejó el callback por la sesión completa. */
  completarOAuth(ticket: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken: ticket })
      .pipe(tap((r) => this.guardarSesion(r)));
  }

  registrar(
    nombre: string,
    correo: string,
    contrasenia: string,
    rolId: number
  ): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/register`,
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
    if (respuesta.refreshToken) {
      localStorage.setItem('refreshToken', respuesta.refreshToken);
    }
    localStorage.setItem(
      'usuario',
      JSON.stringify(respuesta.usuario)
    );
  }

  obtenerToken(): string | null {
    return localStorage.getItem('token');
  }

  obtenerRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  limpiarSesion(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuario');
  }

  // Renueva el access token (corto, 15 min) con el refresh token. Una sola petición en vuelo:
  // si varias llamadas reciben 401 a la vez, todas esperan la misma renovación.
  private renovando$?: Observable<string>;

  refrescar(): Observable<string> {
    if (!this.renovando$) {
      this.renovando$ = this.http
        .post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken: this.obtenerRefreshToken() })
        .pipe(
          tap((r) => this.guardarSesion(r)),
          map((r) => r.token),
          finalize(() => (this.renovando$ = undefined)),
          shareReplay(1)
        );
    }
    return this.renovando$;
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
    // RF-ADM-56: el backend revoca el refresh token y registra el LOGOUT en bitácora.
    // Es "best effort": si falla (por ejemplo sin conexión), igual se cierra la sesión local.
    const refreshToken = this.obtenerRefreshToken();

    if (refreshToken) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe({
        error: () => {
          // Silenciado a propósito: cerrar sesión localmente no debe depender de que el backend responda.
        }
      });
    }

    this.limpiarSesion();
  }

  // RF-ADM-02 y RF-ADM-04: con token vencido la sesión se limpia y se pide login de nuevo.
  estaAutenticado(): boolean {
    const token = this.obtenerToken();

    if (!token) {
      return false;
    }

    if (this.tokenExpirado(token)) {
      // Con refresh token, el interceptor renovará el access token en la primera petición.
      if (this.obtenerRefreshToken()) {
        return true;
      }
      this.limpiarSesion();
      return false;
    }

    return true;
  }
}
