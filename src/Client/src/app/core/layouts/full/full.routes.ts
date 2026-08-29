import { Routes } from '@angular/router';
import { authGuard } from '@ske/auth';

export const fullRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./full').then((m) => m.Full),
    canActivate: [authGuard],
    children: [
      {
        path: 'home',
        loadChildren: () => import('@ske/features/home').then((m) => m.homeRoutes)
      },
      {
        path: 'categories',
        loadChildren: () => import('@ske/features/categories').then((m) => m.categoriesRoutes)
      }
    ]
  }
];
