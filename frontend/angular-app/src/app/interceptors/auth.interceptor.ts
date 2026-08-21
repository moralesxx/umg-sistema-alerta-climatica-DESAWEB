import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const token = localStorage.getItem('token');

  // =========================================================
  // Si no existe token, dejamos pasar la petición normalmente
  // =========================================================

  if (!token) {
    return next(req);
  }

  // =========================================================
  // Agregar JWT al header Authorization
  // =========================================================

  const requestConToken = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(requestConToken);
};