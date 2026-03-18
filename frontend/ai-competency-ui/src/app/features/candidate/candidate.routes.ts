import { Routes } from '@angular/router';

import { candidateAuthGuard } from '../../core/auth/candidate-auth.guard';

export const CANDIDATE_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/candidate-login.component').then(m => m.CandidateLoginComponent)
  },
  {
    path: '',
    loadComponent: () => import('../../layouts/candidate-layout/candidate-layout.component').then(m => m.CandidateLayoutComponent),
    canActivate: [candidateAuthGuard],
    children: [
      {
        path: 'home',
        loadComponent: () => import('./home/candidate-home.component').then(m => m.CandidateHomeComponent)
      },
      {
        path: 'test/:sessionId',
        loadComponent: () => import('./test-runner/test-runner.component').then(m => m.TestRunnerComponent)
      },
      {
        path: 'results/:sessionId',
        loadComponent: () => import('./test-results/test-results.component').then(m => m.TestResultsComponent)
      },
      {
        path: 'adaptive-test/:sessionId',
        loadComponent: () => import('./adaptive-runner/adaptive-runner.component').then(m => m.AdaptiveRunnerComponent)
      },
      {
        path: '',
        redirectTo: 'home',
        pathMatch: 'full'
      }
    ]
  }
];
