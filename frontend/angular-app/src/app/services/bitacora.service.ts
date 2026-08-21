import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RegistrarBitacoraRequest {
  accion: string;
  detalle: string;
}

export interface Bitacora {
  bitacoraId: number;
  usuarioId: number;
  accion: string;
  detalle: string;
  fechaHora: string;
}

@Injectable({
  providedIn: 'root'
})
export class BitacoraService {

  private apiUrl = 'http://localhost:5081/api/bitacora';

  constructor(private http: HttpClient) {}

  /**
   * Registra una acción realizada por el usuario.
   *
   * El UsuarioId NO se envía desde Angular.
   * El backend lo obtiene directamente del JWT.
   */
  registrarAccion(
    accion: string,
    detalle: string
  ): Observable<Bitacora> {

    const request: RegistrarBitacoraRequest = {
      accion,
      detalle
    };

    return this.http.post<Bitacora>(
      this.apiUrl,
      request
    );
  }

  /**
   * Obtiene los últimos registros de la bitácora.
   */
  obtenerBitacora(): Observable<Bitacora[]> {

    return this.http.get<Bitacora[]>(
      this.apiUrl
    );
  }
}