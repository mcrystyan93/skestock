import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('@ske/layouts').then((m) => m.simpleRoutes)
  },
  {
    path: '',
    loadChildren: () => import('@ske/layouts').then((m) => m.fullRoutes)
  }
];
