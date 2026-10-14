import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  // Usuario guardado al iniciar sesión (lectura protegida contra JSON dañado).
  const usuario = authService.estaAutenticado() ? authService.obtenerUsuario() : null;

  if (usuario) {
    // Roles permitidos definidos en las rutas (data: { roles: [...] })
    const rolesPermitidos = route.data['roles'] as Array<string> | undefined;

    // Si la ruta no exige roles específicos o el rol del usuario está incluido, permitimos el paso
    if (!rolesPermitidos || rolesPermitidos.includes(usuario.rol)) {
      return true;
    }
  }

  // Sin permisos suficientes: se redirige al dashboard
  router.navigate(['/dashboard']);
  return false;
};
