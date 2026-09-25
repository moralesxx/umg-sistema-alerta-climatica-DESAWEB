import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OverviewComponent } from '../overview/overview';
import { SensoresComponent } from '../sensores/sensores';
import { AlertasComponent } from '../alertas/alertas';
import { HistorialComponent } from '../historial/historial';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    OverviewComponent,
    SensoresComponent,
    AlertasComponent,
    HistorialComponent
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {}
