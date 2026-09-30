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

  cerrarSesion(): void {
    // RF-ADM-03: cierra sesión (backend + localStorage, vía AuthService)
    // y redirige al login, para que la pantalla cambie de inmediato.
    this.authService.cerrarSesion();
    this.router.navigate(['/login']);
  }
}
