import { Routes } from '@angular/router';

export const schoolClassesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./page/school-classes.page').then((m) => m.SchoolClassesPage)
  },
  {
    path: ':id',
    loadComponent: () => import('./overview/school-class-overview.page').then((m) => m.SchoolClassOverviewPage)
  }
];
