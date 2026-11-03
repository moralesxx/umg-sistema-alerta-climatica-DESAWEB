import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Dispositivo, DispositivoCreado, DispositivoService } from '../../services/dispositivo.service';

@Component({
  selector: 'app-dispositivos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dispositivos.component.html',
  styleUrl: './dispositivos.scss'
})
export class DispositivosComponent implements OnInit {
  dispositivos: Dispositivo[] = [];
  nombre = '';
  prefijo = 'ARD-';
  creado?: DispositivoCreado;
  copiado = false;
  error = '';
  mensaje = '';

  constructor(private svc: DispositivoService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.svc.listar().subscribe({ next: d => { this.dispositivos = d; this.cdr.detectChanges(); }, error: e => this.fallo(e) });
  }

  crear(): void {
    this.limpiar();
    if (!this.nombre.trim()) { this.error = 'Escriba un nombre para el dispositivo.'; return; }
    this.svc.crear(this.nombre.trim(), this.prefijo.trim()).subscribe({
      next: c => { this.creado = c; this.nombre = ''; this.cargar(); },
      error: e => this.fallo(e)
    });
  }

  copiar(): void {
    if (!this.creado) { return; }
    navigator.clipboard?.writeText(this.creado.clave).then(() => { this.copiado = true; this.cdr.detectChanges(); }).catch(() => {});
  }

  ocultarClave(): void { this.creado = undefined; this.copiado = false; }

  alternar(d: Dispositivo): void {
    this.limpiar();
    this.svc.cambiarEstado(d.dispositivoId, !d.activo).subscribe({
      next: () => { this.mensaje = d.activo ? 'Dispositivo revocado: su clave ya no funciona.' : 'Dispositivo activado.'; this.cargar(); },
      error: e => this.fallo(e)
    });
  }

  eliminar(d: Dispositivo): void {
    if (!confirm(`¿Eliminar el dispositivo "${d.nombre}"? Su clave dejará de funcionar.`)) { return; }
    this.limpiar();
    this.svc.eliminar(d.dispositivoId).subscribe({ next: () => { this.mensaje = 'Dispositivo eliminado.'; this.cargar(); }, error: e => this.fallo(e) });
  }

  private limpiar(): void { this.error = ''; this.mensaje = ''; }
  private fallo(e: any): void { this.error = e?.error?.mensaje ?? 'No se pudo completar la operación.'; this.cdr.detectChanges(); }
}
