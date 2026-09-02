import { Routes } from '@angular/router';

export const DATA_IMPORT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/import-page/import-page').then((m) => m.ImportPage),
  },
];
