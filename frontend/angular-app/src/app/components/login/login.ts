import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

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
export class LoginComponent {

  correo = '';
  contrasenia = '';

  mostrarContrasenia = false;
  cargando = false;

  error = '';

  constructor(
    private router: Router,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

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