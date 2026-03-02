import { Routes } from '@angular/router';
import { TestListComponent } from './test-list/test-list.component';

export const TEST_ROUTES: Routes = [
  {
    path: '',
    component: TestListComponent
  },
  {
    path: 'new',
    loadComponent: () => import('./test-builder/test-builder.component').then(m => m.TestBuilderComponent)
  }
];
