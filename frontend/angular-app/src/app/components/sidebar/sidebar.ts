import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss' // Si no tienes archivo scss, puedes eliminar esta línea
})
export class SidebarComponent {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  // RF-ADM-07: la administración de usuarios es solo del Administrador, así
  // que el link se oculta para los demás roles. La seguridad real está en el
  // backend ([Authorize]) y en el roleGuard de la ruta; esto solo evita
  // mostrar un link que no van a poder usar.
  get esAdministrador(): boolean {
    return this.authService.obtenerUsuario()?.rol === 'Administrador';
  }

  cerrarSesion(): void {
    // RF-ADM-03: cierra sesión (backend + localStorage, vía AuthService)
    // y redirige al login, para que la pantalla cambie de inmediato.
    this.authService.cerrarSesion();
    this.router.navigate(['/login']);
  }
}
