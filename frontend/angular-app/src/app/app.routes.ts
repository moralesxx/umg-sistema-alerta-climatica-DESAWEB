import { Routes } from '@angular/router';

import { LoginComponent } from './components/login/login';
import { RegistroComponent } from './components/registro/registro';
import { MainLayoutComponent } from './components/main-layout/main-layout';
import { Dashboard } from './components/dashboard/dashboard';
import { SensoresComponent } from './components/sensores/sensores';
import { AlertasComponent } from './components/alertas/alertas';
import { HistorialComponent } from './components/historial/historial';

import { authGuard } from './core/guards/auth-guard';
import { roleGuard } from './core/guards/role-guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'registro',
    component: RegistroComponent
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'sensores', component: SensoresComponent },
      { path: 'alertas', component: AlertasComponent },
      { path: 'historial', component: HistorialComponent }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
