import { Routes } from '@angular/router';

export const schoolClassesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./school-classes.page').then((m) => m.SchoolClassesPage)
  }
];
