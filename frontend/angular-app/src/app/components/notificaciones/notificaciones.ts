import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfigCorreo, NotificacionService, RegistroEnvio, Suscriptor } from '../../services/notificacion.service';

@Component({
  selector: 'app-notificaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notificaciones.component.html',
  styleUrl: './notificaciones.scss'
})
export class NotificacionesComponent implements OnInit {
  suscriptores: Suscriptor[] = [];
  registro: RegistroEnvio[] = [];
  config?: ConfigCorreo;

  nuevoEmail = '';
  nuevoNombre = '';
  nuevoNivel: 'Amarillo' | 'Naranja' | 'Rojo' = 'Amarillo';

  error = '';
  mensaje = '';
  enviandoPrueba = false;

  constructor(private svc: NotificacionService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.cargar();
    this.svc.config().subscribe({ next: c => { this.config = c; this.cdr.detectChanges(); }, error: () => {} });
  }

  cargar(): void {
    this.svc.suscriptores().subscribe({ next: s => { this.suscriptores = s; this.cdr.detectChanges(); }, error: e => this.fallo(e) });
    this.svc.registro().subscribe({ next: r => { this.registro = r; this.cdr.detectChanges(); }, error: () => {} });
  }

  agregar(): void {
    this.limpiar();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.nuevoEmail.trim())) {
      this.error = 'Escriba un correo válido.';
      return;
    }
    this.svc.agregar({ email: this.nuevoEmail.trim(), nombre: this.nuevoNombre.trim() || null, minNivel: this.nuevoNivel, activo: true }).subscribe({
      next: () => { this.nuevoEmail = ''; this.nuevoNombre = ''; this.mensaje = 'Destinatario agregado.'; this.cargar(); },
      error: e => this.fallo(e)
    });
  }

  alternar(s: Suscriptor): void {
    this.limpiar();
    this.svc.cambiarEstado(s.subId, !s.activo).subscribe({ next: () => this.cargar(), error: e => this.fallo(e) });
  }

  eliminar(s: Suscriptor): void {
    if (!confirm(`¿Quitar a ${s.email} de las notificaciones?`)) { return; }
    this.limpiar();
    this.svc.eliminar(s.subId).subscribe({ next: () => { this.mensaje = 'Destinatario eliminado.'; this.cargar(); }, error: e => this.fallo(e) });
  }

  probar(email?: string): void {
    this.limpiar();
    this.enviandoPrueba = true;
    this.svc.probar(email).subscribe({
      next: r => {
        this.enviandoPrueba = false;
        const fallos = r.resultados.filter(x => !x.enviado);
        if (fallos.length) {
          this.error = 'No se pudo enviar a: ' + fallos.map(f => `${f.email} (${f.error})`).join('; ');
        } else {
          this.mensaje = `Correo de prueba enviado a ${r.resultados.length} destinatario(s).`;
        }
        this.cargar();
      },
      error: e => { this.enviandoPrueba = false; this.fallo(e); }
    });
  }

  private limpiar(): void { this.error = ''; this.mensaje = ''; }

  private fallo(e: any): void {
    this.error = e?.error?.mensaje ?? e?.error?.detail?.mensaje ?? 'No se pudo completar la operación.';
    this.cdr.detectChanges();
  }
}
