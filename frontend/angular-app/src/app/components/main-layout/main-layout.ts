import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar'; // Ajusta la ruta si tu sidebar está en otra carpeta
import { AlarmaGlobalComponent } from '../alarma-global/alarma-global';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, AlarmaGlobalComponent],
  templateUrl: './main.layout.component.html',
  styleUrl: './main.layout.component.scss'
})
export class MainLayoutComponent {}
