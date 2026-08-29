import { Routes } from '@angular/router';
import { guestGuard } from '@ske/auth';

export const simpleRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./simple').then((m) => m.Simple),
    children: [
      {
        path: 'login',
        canActivate: [guestGuard],
        loadChildren: () =>
          import('@ske/features/login').then((m) => m.loginRoutes)
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'login'
      }
    ]
  }
];
