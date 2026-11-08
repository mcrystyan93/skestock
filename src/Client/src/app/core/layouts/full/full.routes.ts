import {Routes} from '@angular/router';
import {authGuard} from '@ske/auth';

export const fullRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./full').then((m) => m.Full),
    canActivate: [authGuard],
    children: [
      {
        path: 'configuration',
        loadComponent: () =>
          import('../../../features/class-configuration/page/configuration.page').then((m) => m.ConfigurationPage)
      },
      {
        path: 'categories',
        loadChildren: () => import('@ske/features/categories').then((m) => m.categoriesRoutes)
      },
      {
        path: 'congregations',
        loadChildren: () => import('@ske/features/congregations').then((m) => m.congregationsRoutes)
      },
      {
        path: 'items',
        loadChildren: () => import('@ske/features/items').then((m) => m.itemsRoutes)
      },
      {
        path: 'school-classes',
        loadChildren: () => import('@ske/features/school-classes').then((m) => m.schoolClassesRoutes)
      }
    ]
  }
];
