import { Routes } from '@angular/router';

import { LoginComponent } from './components/login/login';
import { RegistroComponent } from './components/registro/registro';
import { Dashboard } from './components/dashboard/dashboard';

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
    path: 'dashboard',
    component: Dashboard
  },

  {
    path: '**',
    redirectTo: 'login'
  }

];