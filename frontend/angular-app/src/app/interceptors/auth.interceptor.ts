import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const router = inject(Router);
  const token = localStorage.getItem('token');

  // =========================================================
  // Si no existe token, dejamos pasar la petición normalmente
  // =========================================================

  const requestConToken = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  // =========================================================
  // RF-ADM-04: si el backend responde 401, el token expiró o
  // ya no es válido. Cerramos la sesión y mandamos al login,
  // EXCEPTO si el 401 vino del propio endpoint de login — ese
  // caso ya lo maneja login.ts como "correo o contraseña
  // incorrectos", no como sesión expirada.
  // =========================================================

  return next(requestConToken).pipe(
    catchError((error) => {
      const esLogin = req.url.includes('/login');

      if (error.status === 401 && !esLogin) {
        localStorage.removeItem('token');
        localStorage.removeItem('usuario');
        router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
