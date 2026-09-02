import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'data-import', pathMatch: 'full' },
  {
    path: 'data-import',
    loadChildren: () => import('./features/data-import/data-import.routes').then((m) => m.DATA_IMPORT_ROUTES),
  },
];
