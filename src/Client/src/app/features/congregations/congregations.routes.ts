import { Routes } from '@angular/router';

export const congregationsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./page/congregations.page').then((m) => m.CongregationsPage)
  }
];
