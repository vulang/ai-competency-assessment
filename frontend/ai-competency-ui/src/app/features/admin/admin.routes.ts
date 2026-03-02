import { Routes, RouterLink, RouterLinkActive } from '@angular/router';
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { authGuard } from '../../core/auth/auth.guard';

@Component({
  template: `
    <div class="d-flex h-100vh">
      <!-- Sidebar -->
      <aside class="bg-dark text-white p-3 d-flex flex-column" style="width: 260px; min-height: 100vh;">
        <div class="mb-4 px-2">
           <h5 class="mb-0 text-white font-family-heading">AI Competency</h5>
           <small class="text-white-50">Admin Portal</small>
        </div>

        <nav class="nav flex-column gap-2 flex-grow-1">
           <a routerLink="/admin/dashboard" routerLinkActive="active" class="nav-link text-white-50 d-flex align-items-center rounded px-3 py-2">
              <i class="bi bi-speedometer2 me-2"></i> Dashboard
           </a>
           <a routerLink="/admin/questions" routerLinkActive="active" class="nav-link text-white-50 d-flex align-items-center rounded px-3 py-2">
              <i class="bi bi-collection me-2"></i> Questions
           </a>
           <a routerLink="/admin/questions/generate" routerLinkActive="active" class="nav-link text-white-50 d-flex align-items-center rounded px-3 py-2 ps-4 small">
              <i class="bi bi-magic me-2"></i> AI Generator
           </a>
           <a routerLink="/admin/tests" routerLinkActive="active" class="nav-link text-white-50 d-flex align-items-center rounded px-3 py-2">
              <i class="bi bi-file-earmark-text me-2"></i> Tests
           </a>
           <a routerLink="/admin/rubrics" routerLinkActive="active" class="nav-link text-white-50 d-flex align-items-center rounded px-3 py-2">
              <i class="bi bi-list-check me-2"></i> Rubrics
           </a>
           <a routerLink="/admin/reports/results" routerLinkActive="active" class="nav-link text-white-50 d-flex align-items-center rounded px-3 py-2">
              <i class="bi bi-graph-up me-2"></i> Results
           </a>
        </nav>

        <div class="mt-auto pt-3 border-top border-secondary">
           <div class="d-flex align-items-center mb-3 px-2">
              <div class="rounded-circle bg-primary d-flex align-items-center justify-content-center text-white me-2" style="width: 32px; height: 32px">
                 A
              </div>
              <div>
                 <div class="small fw-bold">{{ auth.currentUser()?.name }}</div>
                 <div class="small text-white-50" style="font-size: 0.75rem">System Admin</div>
              </div>
           </div>
           <button class="btn btn-outline-light w-100 btn-sm" (click)="logout()">Sign Out</button>
        </div>
      </aside>

      <!-- Main Content -->
      <main class="flex-grow-1 bg-light overflow-auto">
         <div class="container-fluid p-4">
            <router-outlet></router-outlet>
         </div>
      </main>
    </div>
  `,
  styles: [`
    .nav-link.active {
       background-color: rgba(255, 255, 255, 0.1);
       color: #fff !important;
       font-weight: 500;
    }
    .nav-link:hover {
       background-color: rgba(255, 255, 255, 0.05);
       color: #fff !important;
    }
    .h-100vh {
       height: 100vh;
    }
  `],
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive]
})
class AdminLayoutComponent {
   auth = inject(AuthService);
   
   logout() {
     this.auth.logout();
   }
}

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
        {
            path: 'questions/generate',
            loadComponent: () => import('./question-generator/question-generator.component').then(m => m.QuestionGeneratorComponent)
        },
        {
            path: 'questions',
            loadChildren: () => import('../questions/questions.routes').then(m => m.QUESTION_ROUTES)
        },
        {
            path: 'tests',
            loadChildren: () => import('../tests/tests.routes').then(m => m.TEST_ROUTES)
        },
        {
            path: 'rubrics',
            loadChildren: () => import('../rubrics/rubrics.routes').then(m => m.RUBRIC_ROUTES)
        },
        {
            path: 'reports',
            loadChildren: () => import('../reports/reports.routes').then(m => m.REPORT_ROUTES)
        },
         {
            path: 'dashboard', // Add direct route to dashboard component if needed via reports
            loadChildren: () => import('../reports/reports.routes').then(m => m.REPORT_ROUTES) // reuse reports for dashboard
        },
        {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
        }
    ]
  }
];
