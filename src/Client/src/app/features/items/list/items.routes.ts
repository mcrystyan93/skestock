import { Routes } from '@angular/router';

export const itemsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./items.page').then((m) => m.ItemsPage)
  }
];
