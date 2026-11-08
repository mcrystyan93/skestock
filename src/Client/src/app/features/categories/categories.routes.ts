import { Routes } from '@angular/router';

export const categoriesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./page/categories.page').then((m) => m.CategoriesPage)
  }
];
