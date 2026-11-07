import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

/**
 * Destino del login con Google/GitHub. El Auth Service redirige aquí con un ticket de un solo uso en el
 * fragmento de la URL (#rt=...). Los fragmentos nunca se envían al servidor ni se guardan en logs de acceso.
 * Se borra de la barra de direcciones de inmediato y se canjea por la sesión completa (access token + refresh).
 */
@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  template: `
    <div class="oauth-callback">
      <div class="spinner-border text-success" role="status"></div>
      <p>Completando inicio de sesión…</p>
    </div>
  `,
  styles: [
    `
      .oauth-callback {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 16px;
        color: #495057;
        font-size: 15px;
      }
    `
  ]
})
export class OauthCallbackComponent implements OnInit {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const ticket = new URLSearchParams(window.location.hash.replace(/^#/, '')).get('rt');

    // El ticket no debe quedar en la barra de direcciones ni en el historial.
    history.replaceState(null, '', window.location.pathname);

    if (!ticket) {
      this.router.navigate(['/login'], { queryParams: { oauth_error: 'estado_invalido' } });
      return;
    }

    this.authService.completarOAuth(ticket).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: () => {
        this.authService.limpiarSesion();
        this.router.navigate(['/login'], { queryParams: { oauth_error: 'estado_invalido' } });
      }
    });
  }
}
