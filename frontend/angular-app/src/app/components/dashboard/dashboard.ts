import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SensorService, Sensor } from '../../services/sensor.service';
import { AlertaService, Alerta } from '../../services/alerta.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  // Indicadores meteorológicos iniciales (simulados en tiempo real)
  temperatura: number = 24.5;
  humedad: number = 68;
  velocidadViento: number = 18.2;
  nivelLluvia: number = 5.4;
  nivelRio: number = 1.8;

  // Estado general y alertas
  nivelAlertaGlobal: string = 'Verde';
  alertasRecientes: Alerta[] = [];
  sensores: Sensor[] = [];

  constructor(
    private sensorService: SensorService,
    private alertaService: AlertaService,
    private cdr: ChangeDetectorRef // <--- Inyectamos el detector de cambios
  ) {}

  ngOnInit(): void {
    this.cargarSensores();
    this.cargarAlertas();
    // Simulación de actualización de sensores en tiempo real (cada 5 segundos)
    setInterval(() => this.simularLecturasTiempoReal(), 5000);
  }

  cargarSensores(): void {
    this.sensorService.getSensores().subscribe({
      next: (data) => {
        this.sensores = data;
        console.log('Sensores cargados:', data);
        this.cdr.detectChanges(); // <--- Forzamos el redibujado de la UI
      },
      error: (err) => console.log('Error al cargar sensores:', err)
    });
  }

  cargarAlertas(): void {
    this.alertaService.getAlertas().subscribe({
      next: (data) => {
        this.alertasRecientes = data;
        console.log('Alertas cargadas:', data);
        this.cdr.detectChanges(); // <--- Forzamos el redibujado de la UI
      },
      error: (err) => console.log('Error al cargar alertas:', err)
    });
  }

  simularLecturasTiempoReal(): void {
    // Variaciones climáticas dinámicas
    this.temperatura = Number((20 + Math.random() * 15).toFixed(1));
    this.humedad = Math.floor(50 + Math.random() * 40);
    this.velocidadViento = Number((10 + Math.random() * 30).toFixed(1));
    this.nivelLluvia = Number((Math.random() * 20).toFixed(1));
    this.nivelRio = Number((1.2 + Math.random() * 2.5).toFixed(2));

    // Lógica para el nivel de riesgo global
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

  getBadgeClass(nivel: string): string {
    if (!nivel) return 'bg-success text-white';
    switch (nivel.toLowerCase()) {
      case 'rojo': case 'critica': return 'bg-danger text-white';
      case 'naranja': return 'bg-warning text-dark';
      case 'amarillo': return 'bg-info text-dark';
      default: return 'bg-success text-white';
    }
  }
}