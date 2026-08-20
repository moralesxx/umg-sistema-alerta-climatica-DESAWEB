import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SensorService, Sensor } from '../../services/sensor.service';
import { AlertaService, Alerta } from '../../services/alerta.service';
import { EventoService, Evento } from '../../services/evento.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  temperatura: number = 24.5;
  humedad: number = 68;
  velocidadViento: number = 18.2;
  nivelLluvia: number = 5.4;
  nivelRio: number = 1.8;

  nivelAlertaGlobal: string = 'Verde';
  alertasRecientes: Alerta[] = [];
  sensores: Sensor[] = [];
  eventos: Evento[] = [];

  constructor(
    private sensorService: SensorService,
    private alertaService: AlertaService,
    private eventoService: EventoService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarSensores();
    this.cargarAlertas();
    this.cargarEventos();
    // Solo actualiza las tarjetas visuales cada 5s, NO inserta en BD
    setInterval(() => this.simularLecturasTiempoReal(), 5000);
  }

  cargarSensores(): void {
    this.sensorService.getSensores().subscribe({
      next: (data) => { this.sensores = data; this.cdr.detectChanges(); },
      error: (err) => console.log('Error sensores:', err)
    });
  }

  cargarAlertas(): void {
    this.alertaService.getAlertas().subscribe({
      next: (data) => { this.alertasRecientes = data; this.cdr.detectChanges(); },
      error: (err) => console.log('Error alertas:', err)
    });
  }

  cargarEventos(): void {
    this.eventoService.getEventos().subscribe({
      next: (data) => { this.eventos = data; this.cdr.detectChanges(); },
      error: (err) => console.log('Error eventos:', err)
    });
  }

  onSimularAlerta(): void {
    const nivelActual = this.nivelAlertaGlobal;
    const mensajePrueba = `Condición climática extrema detectada en campo con nivel ${nivelActual}.`;

    this.alertaService.simularAlerta(nivelActual, mensajePrueba).subscribe({
      next: () => {
        this.cargarAlertas();
        this.cargarEventos();
      },
      error: (err) => console.error('Error al simular:', err)
    });
  }

  onLimpiarHistorial(): void {
    if (confirm('¿Desea limpiar el historial?')) {
      this.alertaService.limpiarHistorial().subscribe({
        next: () => { this.alertasRecientes = []; this.cdr.detectChanges(); },
        error: (err) => console.error('Error limpiar:', err)
      });
    }
  }

  simularLecturasTiempoReal(): void {
    this.temperatura = Number((20 + Math.random() * 15).toFixed(1));
    this.humedad = Math.floor(50 + Math.random() * 40);
    this.velocidadViento = Number((10 + Math.random() * 30).toFixed(1));
    this.nivelLluvia = Number((Math.random() * 20).toFixed(1));
    this.nivelRio = Number((1.2 + Math.random() * 2.5).toFixed(2));

    if (this.nivelRio > 3.2 || this.nivelLluvia > 15) {
      this.nivelAlertaGlobal = 'Rojo';
    } else if (this.nivelRio > 2.5 || this.velocidadViento > 30) {
      this.nivelAlertaGlobal = 'Naranja';
    } else if (this.temperatura > 32 || this.humedad < 30) {
      this.nivelAlertaGlobal = 'Amarillo';
    } else {
      this.nivelAlertaGlobal = 'Verde';
    }
  }

  getBadgeClass(nivel: any): string {
    const n = String(nivel || '').toLowerCase();
    if (n === 'rojo' || n === 'critica') return 'bg-danger text-white';
    if (n === 'naranja') return 'bg-warning text-dark';
    if (n === 'amarillo') return 'bg-warning text-dark fw-bold';
    return 'bg-success text-white';
  }
}
