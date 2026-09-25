import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const roleGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // Obtenemos los datos del usuario guardados al iniciar sesión
  const usuarioStr = localStorage.getItem('usuario');

  if (usuarioStr) {
    const usuario = JSON.parse(usuarioStr);

    // Obtenemos los roles permitidos definidos en las rutas (data: { roles: [...] })
    const rolesPermitidos = route.data['roles'] as Array<string>;

    // Si la ruta no exige roles específicos o el rol del usuario está incluido, permitimos el paso
    if (!rolesPermitidos || rolesPermitidos.includes(usuario.rol)) {
      return true;
    }
  }

  // Si no tiene permisos suficientes, lo redirigimos al dashboard o página principal
  router.navigate(['/dashboard']);
  return false;
};
