import { Routes } from '@angular/router';

import { LoginComponent } from './components/login/login';
import { RegistroComponent } from './components/registro/registro';
import { MainLayoutComponent } from './components/main-layout/main-layout';
import { Dashboard } from './components/dashboard/dashboard';
import { SensoresComponent } from './components/sensores/sensores';
import { ComunidadesComponent } from './components/comunidades/comunidades';
import { UsuariosComponent } from './components/usuarios/usuarios';
import { AlertasComponent } from './components/alertas/alertas';
import { HistorialComponent } from './components/historial/historial';
import { LecturasComponent } from './components/lecturas/lecturas.component';

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
      { path: 'comunidades', component: ComunidadesComponent },
      { path: 'usuarios', component: UsuariosComponent, canActivate: [roleGuard], data: { roles: ['Administrador'] } },
      { path: 'sensores', component: SensoresComponent },
      { path: 'lecturas', component: LecturasComponent },
      { path: 'alertas', component: AlertasComponent },
      { path: 'historial', component: HistorialComponent }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
