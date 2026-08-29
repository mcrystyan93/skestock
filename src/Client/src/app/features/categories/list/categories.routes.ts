import { Routes } from '@angular/router';

export const categoriesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./categories.page').then((m) => m.CategoriesPage)
  }
];
