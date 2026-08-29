import { Routes } from '@angular/router';

export const loginRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./login.page').then((m) => m.LoginPage)
  }
];
