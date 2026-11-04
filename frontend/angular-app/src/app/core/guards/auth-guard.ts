import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  // RF-ADM-04 / RNF-ADM-06: no basta con que exista un token; debe seguir vigente.
  // (estaAutenticado() también limpia la sesión si el token ya expiró.)
  if (authService.estaAutenticado()) {
    return true; // Permite el acceso a la ruta
  }

  // Si no está autenticado, redirige al login
  router.navigate(['/login']);
  return false;
};
