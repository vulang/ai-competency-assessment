import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-candidate-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="home-container">
      <div class="welcome-header">
        <h1>Welcome, {{ userName }}</h1>
        <p>Select an assessment below to get started, or review your past results.</p>
        <div class="action-buttons mt-4">
          <button class="btn btn-primary" (click)="startStandardTest()" [disabled]="isStartingStandard">
             <span *ngIf="isStartingStandard" class="spinner"></span>
             <i class="icon-file"></i> Start Standard Test
          </button>
          <button class="btn btn-success" (click)="startAdaptiveTest()" [disabled]="isStartingAdaptive">
             <span *ngIf="isStartingAdaptive" class="spinner"></span>
             <i class="icon-magic"></i> Start Adaptive Test
          </button>
        </div>
      </div>

      <div class="dashboard-grid">
        <section class="dashboard-section available-tests">
          <h2>Available Assessments</h2>
          <div *ngIf="isLoading" class="loading-state">Loading assessments...</div>
          <div *ngIf="!isLoading && availableTests.length === 0" class="empty-state">
            No assessments are currently available for you.
          </div>
          
          <div class="test-list" *ngIf="availableTests.length > 0">
            <div class="test-card" *ngFor="let test of availableTests">
              <div class="test-info">
                <h3>{{ test.title }}</h3>
                <div class="test-meta">
                  <span class="meta-item">
                    <i class="icon-clock"></i> {{ test.durationMinutes }} mins
                  </span>
                  <span class="meta-item">
                    <i class="icon-file"></i> {{ test.questionCount }} questions
                  </span>
                  <span class="meta-item">
                     Pass Score: {{ test.passScore }}
                  </span>
                </div>
              </div>
              <div class="test-actions">
                <button class="btn btn-primary" (click)="startTest(test.id)" [disabled]="isStartingThis(test.id)">
                  <span *ngIf="isStartingThis(test.id)" class="spinner"></span>
                  Start Test
                </button>
              </div>
            </div>
          </div>
        </section>

        <section class="dashboard-section past-tests">
          <h2>Your Past Results</h2>
          <div *ngIf="isLoadingHistory" class="loading-state">Loading history...</div>
          <div *ngIf="!isLoadingHistory && pastTests.length === 0" class="empty-state">
            You haven't completed any assessments yet.
          </div>

          <div class="history-list" *ngIf="pastTests.length > 0">
            <div class="history-item" *ngFor="let test of pastTests">
              <div class="history-info">
                <h4>{{ test.examTitle }}</h4>
                <div class="history-date">{{ test.endTime | date:'medium' }}</div>
              </div>
              <div class="history-score">
                <div class="score-value" [class.passed]="test.totalScore >= test.passScore" [class.failed]="test.totalScore < test.passScore">
                  {{ test.totalScore }}/{{ test.passScore }}
                </div>
                <button class="btn btn-text" (click)="viewResult(test.sessionId)">View Feedback</button>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .home-container {
      font-family: 'Inter', sans-serif;
    }
    .welcome-header {
      margin-bottom: 40px;
    }
    .welcome-header h1 {
      font-size: 2rem;
      color: #1a202c;
      margin: 0 0 8px 0;
    }
    .mt-4 {
      margin-top: 16px;
    }
    .action-buttons {
      display: flex;
      gap: 16px;
      margin-top: 16px;
    }
    .btn-success {
      background: #38a169;
      color: #fff;
      font-size: 1.1rem;
      padding: 12px 24px;
    }
    .btn-success:hover {
      background: #2f855a;
    }
    .welcome-header p {
      color: #718096;
      font-size: 1.1rem;
      margin: 0;
    }
    .dashboard-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 32px;
    }
    @media (max-width: 992px) {
      .dashboard-grid {
        grid-template-columns: 1fr;
      }
    }
    .dashboard-section h2 {
      font-size: 1.25rem;
      color: #2d3748;
      border-bottom: 2px solid #e2e8f0;
      padding-bottom: 12px;
      margin-bottom: 24px;
    }
    .test-card {
      background: #fff;
      border-radius: 8px;
      padding: 24px;
      margin-bottom: 16px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.05);
      border: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .test-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    }
    .test-info h3 {
      margin: 0 0 12px 0;
      font-size: 1.25rem;
      color: #1a202c;
    }
    .test-meta {
      display: flex;
      gap: 16px;
      color: #718096;
      font-size: 0.9rem;
    }
    .meta-item {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .btn {
      padding: 8px 16px;
      border-radius: 6px;
      font-weight: 500;
      cursor: pointer;
      border: none;
      transition: all 0.2s;
      display: inline-flex;
      align-items: center;
      gap: 8px;
    }
    .btn-primary {
      background: #3182ce;
      color: #fff;
    }
    .btn-primary:hover {
      background: #2b6cb0;
    }
    .btn:disabled {
      opacity: 0.7;
      cursor: not-allowed;
    }
    .btn-text {
      background: transparent;
      color: #3182ce;
      padding: 4px 8px;
    }
    .btn-text:hover {
      background: #ebf8ff;
    }
    .history-item {
      background: #fff;
      border-radius: 8px;
      padding: 16px;
      margin-bottom: 12px;
      border: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .history-info h4 {
      margin: 0 0 4px 0;
      font-size: 1rem;
      color: #2d3748;
    }
    .history-date {
      font-size: 0.8rem;
      color: #a0aec0;
    }
    .history-score {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 8px;
    }
    .score-value {
      font-weight: 700;
      font-size: 1.1rem;
    }
    .passed { color: #38a169; }
    .failed { color: #e53e3e; }
    .empty-state, .loading-state {
      padding: 32px;
      text-align: center;
      color: #718096;
      background: #f7fafc;
      border-radius: 8px;
      border: 1px dashed #cbd5e0;
    }
    .spinner {
      border: 2px solid rgba(255,255,255,0.3);
      width: 14px;
      height: 14px;
      border-radius: 50%;
      border-left-color: #fff;
      animation: spin 1s ease infinite;
    }
    @keyframes spin { 100% { transform: rotate(360deg); } }
  `]
})
export class CandidateHomeComponent implements OnInit {
  userName = 'Test Taker';
  availableTests: any[] = [];
  pastTests: any[] = [];
  isLoading = true;
  isLoadingHistory = true;
  startingExamId: number | null = null;
  isStartingAdaptive = false;
  isStartingStandard = false;
  
  private http = inject(HttpClient);
  private router = inject(Router);
  private authService = inject(AuthService);

  ngOnInit() {
    const user = this.authService.currentUser();
    if (user) {
      this.userName = user.name || user.username;
    }

    this.loadAvailableTests();
    this.loadPastTests();
  }

  get headers() {
    const user = this.authService.currentUser();
    return {
      Authorization: `Bearer ${user?.accessToken || sessionStorage.getItem('access_token')}`
    };
  }

  loadAvailableTests() {
    this.http.get<any[]>(`${environment.apiUrl}/api/candidate/tests/available`, { headers: this.headers })
      .subscribe({
        next: (tests) => {
          this.availableTests = tests;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load tests', err);
          this.isLoading = false;
        }
      });
  }

  loadPastTests() {
    this.http.get<any[]>(`${environment.apiUrl}/api/candidate/tests/history`, { headers: this.headers })
      .subscribe({
        next: (history) => {
          this.pastTests = history;
          this.isLoadingHistory = false;
        },
        error: (err) => {
          console.error('Failed to load history', err);
          this.isLoadingHistory = false;
        }
      });
  }

  startTest(examId: number) {
    this.startingExamId = examId;
    this.http.post<any>(`${environment.apiUrl}/api/candidate/tests/${examId}/start`, {}, { headers: this.headers })
      .subscribe({
        next: (response) => {
          this.router.navigate(['/candidate/test', response.sessionId]);
        },
        error: (err) => {
          console.error('Failed to start test', err);
          this.startingExamId = null;
          alert('Failed to start the test. Please try again.');
        }
      });
  }

  isStartingThis(examId: number) {
    return this.startingExamId === examId;
  }

  viewResult(sessionId: string) {
    this.router.navigate(['/candidate/results', sessionId]);
  }

  startStandardTest() {
    this.isStartingStandard = true;
    
    this.http.post<any>(`${environment.apiUrl}/api/candidate/tests/start-standard`, {}, { headers: this.headers })
      .subscribe({
        next: (response) => {
          this.router.navigate(['/candidate/test', response.sessionId]);
        },
        error: (err) => {
          console.error('Failed to start standard test', err);
          this.isStartingStandard = false;
          alert('Failed to start the standard test. Please try again.');
        }
      });
  }

  startAdaptiveTest() {
    this.isStartingAdaptive = true;
    this.http.post<any>(`${environment.apiUrl}/api/adaptivetests/start`, {}, { headers: this.headers })
      .subscribe({
        next: (response) => {
          this.router.navigate(['/candidate/adaptive-test', response.sessionId]);
        },
        error: (err) => {
          console.error('Failed to start adaptive test', err);
          this.isStartingAdaptive = false;
          alert('Failed to start the adaptive test. Please try again.');
        }
      });
  }
}
