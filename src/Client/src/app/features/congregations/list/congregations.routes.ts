import { Routes } from '@angular/router';

export const congregationsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./congregations.page').then((m) => m.CongregationsPage)
  }
];
