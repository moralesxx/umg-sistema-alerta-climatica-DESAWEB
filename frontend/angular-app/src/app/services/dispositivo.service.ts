import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Dispositivo {
  dispositivoId: number;
  nombre: string;
  prefijo?: string | null;
  activo: boolean;
  creado: string;
  ultimoContacto?: string | null;
}

export interface DispositivoCreado { dispositivoId: number; nombre: string; prefijo?: string | null; clave: string; aviso: string; }

// Dispositivos IoT (Arduino/ESP32): cada uno tiene su propia clave, revocable, para la API de ingesta.
@Injectable({ providedIn: 'root' })
export class DispositivoService {
  private apiUrl = '/api/sensors/devices';

  constructor(private http: HttpClient) {}

  listar(): Observable<Dispositivo[]> { return this.http.get<Dispositivo[]>(this.apiUrl); }
  crear(nombre: string, prefijo?: string): Observable<DispositivoCreado> { return this.http.post<DispositivoCreado>(this.apiUrl, { nombre, prefijo: prefijo || null }); }
  cambiarEstado(id: number, activo: boolean): Observable<any> { return this.http.patch(`${this.apiUrl}/${id}/estado`, activo); }
  eliminar(id: number): Observable<any> { return this.http.delete(`${this.apiUrl}/${id}`); }
}
