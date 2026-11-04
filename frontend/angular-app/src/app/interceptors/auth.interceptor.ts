import { HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Endpoints de autenticación: no llevan token ni disparan renovación (evita bucles).
const esRutaDeAuth = (url: string) =>
  url.includes('/api/auth/login') || url.includes('/api/auth/register') ||
  url.includes('/api/auth/refresh') || url.includes('/api/auth/logout');

const conToken = (req: HttpRequest<unknown>, token: string | null) =>
  token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);

  if (esRutaDeAuth(req.url)) {
    return next(req);
  }

  // RF-ADM-04: el access token es corto (15 min). Si el backend responde 401:
  //  1) con refresh token se renueva UNA vez (compartido entre peticiones simultáneas) y se reintenta;
  //  2) si no hay refresh token o la renovación falla, se cierra la sesión y se envía al login.
  return next(conToken(req, auth.obtenerToken())).pipe(
    catchError((error) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      if (!auth.obtenerRefreshToken()) {
        auth.limpiarSesion();
        router.navigate(['/login']);
        return throwError(() => error);
      }

      return auth.refrescar().pipe(
        switchMap((nuevoToken) => next(conToken(req, nuevoToken))),
        catchError((errorRenovacion) => {
          auth.limpiarSesion();
          router.navigate(['/login']);
          return throwError(() => errorRenovacion);
        })
      );
    })
  );
};
