import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-registro',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './registro.html',
  styleUrl: './registro.scss'
})
export class RegistroComponent {

  nombre = '';
  correo = '';
  contrasenia = '';
  confirmarContrasenia = '';

  mostrarContrasenia = false;
  mostrarConfirmacion = false;

  cargando = false;

  error = '';
  mensaje = '';

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  registrar(): void {

    this.error = '';
    this.mensaje = '';

    if (
      !this.nombre.trim() ||
      !this.correo.trim() ||
      !this.contrasenia ||
      !this.confirmarContrasenia
    ) {
      this.error = 'Todos los campos son obligatorios.';
      return;
    }

    if (this.contrasenia.length < 8 || this.contrasenia.length > 72 || !/[A-Za-z]/.test(this.contrasenia) || !/\d/.test(this.contrasenia)) {
      this.error =
        'La contraseña debe tener entre 8 y 72 caracteres e incluir letras y números.';
      return;
    }

    if (this.contrasenia !== this.confirmarContrasenia) {
      this.error =
        'Las contraseñas no coinciden.';
      return;
    }

    this.cargando = true;

    // Todo usuario registrado desde el formulario
    // comienza como Visualizador.
    const rolId = 3;

    this.authService
      .registrar(
        this.nombre.trim(),
        this.correo.trim(),
        this.contrasenia,
        rolId
      )
      .subscribe({

        next: (respuesta: any) => {

          this.cargando = false;

          // La cuenta queda pendiente: un administrador debe activarla antes de poder iniciar sesión.
          this.mensaje =
            respuesta?.mensaje ||
            'Solicitud registrada. Un administrador debe activar su cuenta.';

          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 3500);
        },

        error: (error) => {

          this.cargando = false;

          if (error.status === 409) {
            this.error =
              error.error?.mensaje ||
              'Ya existe un usuario con ese correo.';
          }
          else if (error.status === 400) {
            this.error =
              error.error?.mensaje ||
              'Los datos proporcionados no son válidos.';
          }
          else if (error.status === 0) {
            this.error =
              'No se pudo conectar con el servidor.';
          }
          else {
            this.error =
              error.error?.mensaje ||
              'Ocurrió un error al registrar el usuario.';
          }
        }

      });
  }

  regresarLogin(): void {
    this.router.navigate(['/login']);
  }
}