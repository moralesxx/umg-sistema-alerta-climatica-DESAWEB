import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Suscriptor {
  subId: number;
  email: string;
  nombre?: string | null;
  minNivel: 'Amarillo' | 'Naranja' | 'Rojo';
  comunidadIds: number[];
  activo: boolean;
}

export interface RegistroEnvio {
  fecha: string;
  email: string;
  alertaId?: number | null;
  asunto?: string | null;
  estado: 'SENT' | 'FAILED' | 'SKIPPED';
  error?: string | null;
}

export interface ConfigCorreo { host: string; port: number; seguridad: string; autenticado: boolean; remitente: string; }

// Notification Service: avisos de alertas por correo (el envío ocurre en el backend).
@Injectable({ providedIn: 'root' })
export class NotificacionService {
  private apiUrl = '/api/notifications';

  constructor(private http: HttpClient) {}

  suscriptores(): Observable<Suscriptor[]> {
    return this.http.get<Suscriptor[]>(`${this.apiUrl}/subscribers`);
  }

  agregar(s: Partial<Suscriptor>): Observable<Suscriptor> {
    return this.http.post<Suscriptor>(`${this.apiUrl}/subscribers`, s);
  }

  cambiarEstado(id: number, activo: boolean): Observable<Suscriptor> {
    return this.http.patch<Suscriptor>(`${this.apiUrl}/subscribers/${id}/estado`, null, { params: new HttpParams().set('activo', activo) });
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/subscribers/${id}`);
  }

  probar(email?: string): Observable<{ resultados: { email: string; enviado: boolean; error?: string | null }[] }> {
    return this.http.post<any>(`${this.apiUrl}/test`, email ? { email } : {});
  }

  registro(): Observable<RegistroEnvio[]> {
    return this.http.get<RegistroEnvio[]>(`${this.apiUrl}/log`, { params: new HttpParams().set('limite', 30) });
  }

  config(): Observable<ConfigCorreo> {
    return this.http.get<ConfigCorreo>(`${this.apiUrl}/config`);
  }
}
