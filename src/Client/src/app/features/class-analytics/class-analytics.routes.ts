import { Routes } from '@angular/router';

export const classAnalyticsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./class-analytics.page').then((m) => m.ClassAnalyticsPage)
  }
];
