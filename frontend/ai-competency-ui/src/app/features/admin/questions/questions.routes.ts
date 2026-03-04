import { Routes } from '@angular/router';
import { QuestionListComponent } from './question-list/question-list.component';

export const QUESTION_ROUTES: Routes = [
  {
    path: '',
    component: QuestionListComponent
  },

  {
      path: 'new',
      loadComponent: () => import('./question-form/question-form.component').then(m => m.QuestionFormComponent)
  },
  {
      path: ':id/edit',
      loadComponent: () => import('./question-form/question-form.component').then(m => m.QuestionFormComponent)
  }
];
