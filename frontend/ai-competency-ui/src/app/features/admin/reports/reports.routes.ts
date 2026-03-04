import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { ExamResultsComponent } from './exam-results/exam-results.component';

export const REPORT_ROUTES: Routes = [
  {
    path: 'dashboard',
    component: DashboardComponent
  },
  {
    path: 'results',
    component: ExamResultsComponent
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  }
];
