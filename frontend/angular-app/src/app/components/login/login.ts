import { Component, ChangeDetectorRef, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, ProveedorOAuth } from '../../services/auth.service';

// Códigos que devuelve el Auth Service en ?oauth_error=... (nunca texto libre del proveedor).
const MENSAJES_OAUTH: Record<string, string> = {
  acceso_denegado: 'Cancelaste el acceso con el proveedor externo.',
  estado_invalido: 'La solicitud de inicio de sesión expiró o no es válida. Inténtalo de nuevo.',
  cuenta_inactiva: 'Tu cuenta fue registrada, pero está pendiente de aprobación por un administrador (o está desactivada).',
  email_no_verificado: 'El proveedor no entregó un correo verificado. Verifica tu correo en Google/GitHub e inténtalo de nuevo.',
  proveedor_no_disponible: 'Ese método de acceso no está habilitado en este momento.',
  proveedor_error: 'No se pudo completar el inicio de sesión con el proveedor. Inténtalo de nuevo.'
};

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent implements OnInit {

  /** Proveedores OAuth habilitados en el servidor (vacío = no se muestran botones). */
  proveedoresOAuth: ProveedorOAuth[] = [];

  correo = '';
  contrasenia = '';

  mostrarContrasenia = false;
  cargando = false;

  error = '';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Si volvemos de un intento fallido con Google/GitHub, se explica el motivo.
    const codigo = this.route.snapshot.queryParamMap.get('oauth_error');
    if (codigo) {
      this.error = MENSAJES_OAUTH[codigo] ?? 'No se pudo iniciar sesión con el proveedor externo.';
    }

    this.authService.oauthProveedores().subscribe({
      next: (lista) => {
        this.proveedoresOAuth = lista;
        this.cdr.detectChanges();
      },
      error: () => {
        // Sin conexión con el Auth Service: simplemente no se ofrecen los botones externos.
        this.proveedoresOAuth = [];
      }
    });
  }

  // Si el usuario vuelve atrás desde la página del proveedor (caché del navegador), se rehabilitan los botones.
  @HostListener('window:pageshow')
  alMostrarPagina(): void {
    this.cargando = false;
  }

  iniciarConProveedor(proveedor: ProveedorOAuth): void {
    if (this.cargando) {
      return;
    }
    this.cargando = true;
    this.authService.iniciarOAuth(proveedor);
  }

  iniciarSesion(): void {

    this.error = '';

    // ==============================
    // VALIDAR CORREO
    // ==============================

    if (!this.correo.trim()) {
      this.error = 'Debe ingresar el correo electrónico.';
      return;
    }

    // ==============================
    // VALIDAR CONTRASEÑA
    // ==============================

    if (!this.contrasenia.trim()) {
      this.error = 'Debe ingresar la contraseña.';
      return;
    }

    // Evitar múltiples peticiones
    if (this.cargando) {
      return;
    }

    this.cargando = true;

    this.authService.login(
      this.correo.trim(),
      this.contrasenia
    )
    .subscribe({

      // ==========================================
      // LOGIN CORRECTO
      // ==========================================

      next: (respuesta) => {

        console.log('Login exitoso:', respuesta);

        this.cargando = false;

        this.authService.guardarSesion(respuesta);

        this.router.navigate(['/dashboard']);
      },

      // ==========================================
      // ERROR
      // ==========================================

      error: (error) => {

        console.log('========== ERROR LOGIN ==========');
        console.log('STATUS:', error.status);
        console.log('ERROR:', error);
        console.log('ANTES cargando:', this.cargando);

        this.cargando = false;

        this.contrasenia = '';

        if (error.status === 401) {

          this.error =
            error.error?.mensaje ??
            'Correo o contraseña incorrectos.';

        }
        else if (error.status === 400) {

          this.error =
            error.error?.mensaje ??
            'Los datos ingresados no son válidos.';

        }
        else if (error.status === 0) {

          this.error =
            'No se pudo conectar con el servidor. Verifica que la API esté ejecutándose.';

        }
        else {

          this.error =
            error.error?.mensaje ??
            'Ocurrió un error al iniciar sesión.';
        }

        console.log('DESPUÉS cargando:', this.cargando);
        console.log('MENSAJE:', this.error);

        // Forzar actualización visual de Angular
        this.cdr.detectChanges();
      }

    });
  }

  irARegistro(): void {
    this.router.navigate(['/registro']);
  }

  recuperarContrasenia(): void {

    alert(
      'La recuperación de contraseña se implementará posteriormente.'
    );
  }
}