import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { AlertaService } from '../../services/alerta.service';
import { DashboardService } from '../../services/dashboard.service';

// Alarma global: mientras el nivel de riesgo sea NARANJA (aviso naranja, pitido espaciado) o ROJO (aviso rojo,
// sirena rápida) se muestra un aviso intermitente en TODAS las pantallas y suena en el navegador (se puede silenciar). El buzzer del Arduino se activa
// con el mismo nivel desde ArduinoSerialService.
@Component({
  selector: 'app-alarma-global',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div *ngIf="alarma" class="alarma" [class.alarma-roja]="alarma === 'rojo'" [class.alarma-naranja]="alarma === 'naranja'" role="alert">
      <span class="alarma-icono">{{ alarma === 'rojo' ? '🚨' : '⚠️' }}</span>
      <strong>{{ alarma === 'rojo' ? 'ALERTA ROJA ACTIVA' : 'ALERTA NARANJA ACTIVA' }}</strong>
      <span class="d-none d-md-inline">— {{ alarma === 'rojo' ? 'riesgo crítico detectado' : 'riesgo alto detectado' }}; revise las alertas y atiéndalas.</span>
      <a routerLink="/alertas" class="btn btn-sm btn-light ms-3">Ver alertas</a>
      <button type="button" class="btn btn-sm btn-outline-light ms-2" (click)="alternarSonido()">
        {{ silenciado ? '🔇 Sonido apagado' : '🔊 Silenciar' }}
      </button>
    </div>
  `,
  styles: [`
    .alarma {
      position: fixed; top: 0; left: 0; right: 0; z-index: 2000;
      display: flex; align-items: center; justify-content: center; flex-wrap: wrap; gap: .5rem;
      padding: .55rem 1rem; color: #fff;
    }
    .alarma-roja { background: #dc3545; animation: parpadeo-alarma 1s steps(2, start) infinite; box-shadow: 0 2px 12px rgba(220, 53, 69, .7); }
    .alarma-naranja { background: #fd7e14; animation: parpadeo-naranja 2s steps(2, start) infinite; box-shadow: 0 2px 12px rgba(253, 126, 20, .7); }
    @keyframes parpadeo-naranja { 50% { background: #b85a00; } }
    .alarma-icono { font-size: 1.3rem; }
    @keyframes parpadeo-alarma { 50% { background: #7a0f1a; } }
  `]
})
export class AlarmaGlobalComponent implements OnInit, OnDestroy {
  alarma: 'rojo' | 'naranja' | null = null;
  silenciado = false;

  private subs: Subscription[] = [];
  private ultimaActualizacion = 0;
  private audio?: AudioContext;
  private sirena?: ReturnType<typeof setInterval>;
  private agudo = false;

  constructor(
    private alertaService: AlertaService,
    private dashboardService: DashboardService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    try { this.silenciado = localStorage.getItem('alarmaSilenciada') === '1'; } catch { /* sin almacenamiento */ }

    this.subs.push(this.alertaService.nivelAlerta$.subscribe((nivel) => {
      this.ultimaActualizacion = Date.now();
      const n = String(nivel || '').toLowerCase();
      this.aplicarNivel(n === 'rojo' ? 'rojo' : n === 'naranja' ? 'naranja' : null);
    }));

    // Consulta propia cada 5 s para que la alarma funcione en cualquier pantalla;
    // si otro componente (p. ej. el dashboard) ya actualizó el nivel hace poco, no repite la consulta.
    this.consultar();
    this.subs.push(interval(5000).subscribe(() => {
      if (Date.now() - this.ultimaActualizacion > 4500) this.consultar();
    }));
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.detenerSirena();
    try { this.audio?.close(); } catch { /* ya cerrado */ }
  }

  alternarSonido(): void {
    this.silenciado = !this.silenciado;
    try { localStorage.setItem('alarmaSilenciada', this.silenciado ? '1' : '0'); } catch { /* sin almacenamiento */ }
    if (this.silenciado) this.detenerSirena();
    else if (this.alarma) this.iniciarSirena();
  }

  private consultar(): void {
    this.dashboardService.getResumen(undefined, 1).subscribe({
      next: (d) => this.alertaService.setNivelAlerta(d.nivelRiesgoGlobal),
      error: () => { /* si falla la consulta se conserva el último estado */ }
    });
  }

  private aplicarNivel(nivel: 'rojo' | 'naranja' | null): void {
    if (nivel === this.alarma) return;
    this.alarma = nivel;
    this.detenerSirena();   // el patrón cambia según el nivel
    if (nivel && !this.silenciado) this.iniciarSirena();
    this.cdr.detectChanges();
  }

  private iniciarSirena(): void {
    if (this.sirena) return;
    this.sirena = setInterval(() => this.pitido(), this.alarma === 'rojo' ? 600 : 1500);
    this.pitido();
  }

  private detenerSirena(): void {
    if (this.sirena) { clearInterval(this.sirena); this.sirena = undefined; }
  }

  // Dos tonos alternados. Los navegadores solo permiten sonido tras una interacción del usuario
  // (el inicio de sesión ya cuenta); si aún está bloqueado, el aviso visual sigue funcionando.
  private pitido(): void {
    try {
      this.audio ??= new AudioContext();
      if (this.audio.state === 'suspended') void this.audio.resume();
      const osc = this.audio.createOscillator();
      const vol = this.audio.createGain();
      this.agudo = !this.agudo;
      osc.type = 'square';
      osc.frequency.value = this.alarma === 'rojo' ? (this.agudo ? 1100 : 760) : 880;
      vol.gain.value = 0.08;
      osc.connect(vol);
      vol.connect(this.audio.destination);
      osc.start();
      osc.stop(this.audio.currentTime + 0.45);
    } catch { /* audio no disponible */ }
  }
}
