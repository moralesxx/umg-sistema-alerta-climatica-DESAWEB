import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // Verificamos si existe el token en el almacenamiento local
  const token = localStorage.getItem('token');

  if (token) {
    return true; // Permite el acceso a la ruta
  }

  // Si no está autenticado, redirige al login
  router.navigate(['/login']);
  return false;
};
