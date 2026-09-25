import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { BitacoraService } from '../../services/bitacora.service';
import { AlertaService } from '../../services/alerta.service';

@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './overview.component.html',
  styleUrl: './overview.scss'
})
export class OverviewComponent implements OnInit, OnDestroy {
  temperatura: number = 24.5;
  humedad: number = 68;
  velocidadViento: number = 18.2;
  nivelLluvia: number = 5.4;
  nivelRio: number = 1.8;
  nivelAlertaGlobal: string = 'Verde';

  historialTemperaturas: number[] = [22, 23, 24, 24.5];
  historialRios: number[] = [1.5, 1.6, 1.7, 1.8];
  svgPathTemperatura: string = '';
  svgPathRio: string = '';

  private intervaloTiempoReal: ReturnType<typeof setInterval> | null = null;

  constructor(
    private authService: AuthService,
    private bitacoraService: BitacoraService,
    private alertaService: AlertaService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.actualizarGraficos();
    this.alertaService.setNivelAlerta(this.nivelAlertaGlobal);
    this.intervaloTiempoReal = setInterval(() => {
      this.simularLecturasTiempoReal();
      this.actualizarGraficos();
      this.cdr.detectChanges();
    }, 5000);
  }

  ngOnDestroy(): void {
    if (this.intervaloTiempoReal !== null) {
      clearInterval(this.intervaloTiempoReal);
      this.intervaloTiempoReal = null;
    }
  }

  cerrarSesion(): void {
    this.bitacoraService.registrarAccion('CERRAR_SESION', 'El usuario cerró sesión desde el dashboard.').subscribe({
      next: () => {
        this.authService.cerrarSesion();
        window.location.href = '/login';
      },
      error: () => {
        this.authService.cerrarSesion();
        window.location.href = '/login';
      }
    });
  }

  simularLecturasTiempoReal(): void {
    this.temperatura = Number((20 + Math.random() * 15).toFixed(1));
    this.humedad = Math.floor(50 + Math.random() * 40);
    this.velocidadViento = Number((10 + Math.random() * 30).toFixed(1));
    this.nivelLluvia = Number((Math.random() * 20).toFixed(1));
    this.nivelRio = Number((1.2 + Math.random() * 2.5).toFixed(2));

    this.historialTemperaturas.push(this.temperatura);
    this.historialRios.push(this.nivelRio);

    if (this.historialTemperaturas.length > 10) this.historialTemperaturas.shift();
    if (this.historialRios.length > 10) this.historialRios.shift();

    const orden = ['Verde', 'Amarillo', 'Naranja', 'Rojo'];
    const rand = Math.random();
    let nivelBase = rand < 0.25 ? 'Verde' : rand < 0.50 ? 'Amarillo' : rand < 0.75 ? 'Naranja' : 'Rojo';

    let nivelSensores = 'Verde';
    if (this.nivelRio > 3.2 || this.nivelLluvia > 15) {
      nivelSensores = 'Rojo';
    } else if (this.nivelRio > 2.7 || this.velocidadViento > 35) {
      nivelSensores = 'Naranja';
    } else if (this.temperatura > 32 || this.humedad < 52) {
      nivelSensores = 'Amarillo';
    }

    const indiceFinal = Math.max(orden.indexOf(nivelBase), orden.indexOf(nivelSensores));
    this.nivelAlertaGlobal = orden[indiceFinal];
    this.alertaService.setNivelAlerta(this.nivelAlertaGlobal);
  }

  actualizarGraficos(): void {
    this.svgPathTemperatura = this.generarPathSvg(this.historialTemperaturas, 10, 40);
    this.svgPathRio = this.generarPathSvg(this.historialRios, 0, 5);
  }

  private generarPathSvg(datos: number[], minVal: number, maxVal: number): string {
    if (!datos || datos.length === 0) return '';
    const width = 500;
    const height = 150;
    const step = width / Math.max(datos.length - 1, 1);

    return datos.map((val, i) => {
      const x = i * step;
      const normalizedVal = Math.min(Math.max(val, minVal), maxVal);
      const y = height - ((normalizedVal - minVal) / (maxVal - minVal)) * height;
      return `${i === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)}`;
    }).join(' ');
  }

  getBadgeClass(texto: any): string {
    const n = String(texto || '').toLowerCase();
    if (n.includes('rojo') || n.includes('critica') || n.includes('crítica')) return 'bg-danger text-white';
    if (n.includes('naranja')) return 'bg-warning-orange text-white';
    if (n.includes('amarillo')) return 'bg-warning text-dark fw-bold';
    if (n.includes('verde')) return 'bg-success text-white';
    return 'bg-secondary text-white';
  }
}
