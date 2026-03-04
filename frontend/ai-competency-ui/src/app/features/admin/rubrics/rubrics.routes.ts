import { Routes } from '@angular/router';
import { RubricListComponent } from './rubric-list/rubric-list.component';

export const RUBRIC_ROUTES: Routes = [
  {
    path: '',
    component: RubricListComponent
  },
  {
    path: 'new',
    loadComponent: () => import('./rubric-form/rubric-form.component').then(m => m.RubricFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./rubric-form/rubric-form.component').then(m => m.RubricFormComponent)
  }
];
