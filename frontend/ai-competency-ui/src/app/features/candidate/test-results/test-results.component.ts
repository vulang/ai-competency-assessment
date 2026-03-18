import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-test-results',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="results-container" *ngIf="result && !isLoading">
      <div class="results-header">
        <button class="btn btn-text back-btn" (click)="goHome()">
          <i class="icon-arrow-left"></i> Back to Dashboard
        </button>
        <h2>{{ result.examTitle }} Results</h2>
      </div>

      <div class="score-card">
        <div class="score-circle" [class.passed]="result.passed" [class.failed]="!result.passed">
          <div class="score-value">{{ result.totalScore }}</div>
          <div class="score-total">out of {{ result.passScore }}+ needed</div>
        </div>
        <div class="score-status">
          <h3 [class.text-passed]="result.passed" [class.text-failed]="!result.passed">
            {{ result.passed ? 'Passed!' : 'Did not pass' }}
          </h3>
          <p>Completed on {{ result.endTime | date:'medium' }}</p>
        </div>
      </div>

      <div class="feedback-section">
        <h3>Detailed Feedback</h3>
        
        <div class="question-feedback" *ngFor="let response of result.responses; let i = index">
          <div class="q-header">
            <h4>Question {{ i + 1 }}</h4>
            <span class="q-score" [class.full-score]="response.scoreEarned > 0" [class.zero-score]="response.scoreEarned === 0">
              {{ response.scoreEarned }} pts
            </span>
          </div>
          <p class="q-content">{{ response.content }}</p>
          
          <div class="response-diff">
            <div class="user-answer">
              <strong>Your Answer:</strong>
              <div class="answer-box">{{ formatAnswer(response.finalAnswer) }}</div>
            </div>
            <div class="ai-feedback" *ngIf="response.aiFeedback">
              <strong>AI Feedback:</strong>
              <div class="feedback-box">{{ response.aiFeedback }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="loading-state" *ngIf="isLoading">
      <div class="spinner-large"></div>
      <p>Loading results...</p>
    </div>
  `,
  styles: [`
    .results-container {
      max-width: 800px;
      margin: 0 auto;
      font-family: 'Inter', sans-serif;
    }
    .results-header {
      margin-bottom: 24px;
    }
    .results-header h2 {
      font-size: 1.5rem;
      color: #1a202c;
      margin: 16px 0 0 0;
    }
    .back-btn {
      color: #718096;
      padding: 0;
      display: inline-flex;
      align-items: center;
      gap: 8px;
    }
    .back-btn:hover {
      color: #2d3748;
      background: transparent;
    }
    .score-card {
      background: #ffffff;
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.05);
      border: 1px solid #e2e8f0;
      padding: 32px;
      display: flex;
      align-items: center;
      gap: 32px;
      margin-bottom: 32px;
    }
    .score-circle {
      width: 120px;
      height: 120px;
      border-radius: 50%;
      border: 8px solid #e2e8f0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .score-circle.passed {
      border-color: #48bb78;
    }
    .score-circle.failed {
      border-color: #f56565;
    }
    .score-value {
      font-size: 2rem;
      font-weight: 700;
      color: #1a202c;
      line-height: 1;
    }
    .score-total {
      font-size: 0.8rem;
      color: #718096;
      margin-top: 4px;
    }
    .score-status h3 {
      font-size: 1.5rem;
      margin: 0 0 8px 0;
    }
    .text-passed { color: #38a169; }
    .text-failed { color: #e53e3e; }
    .score-status p {
      color: #718096;
      margin: 0;
    }
    .feedback-section h3 {
      font-size: 1.25rem;
      color: #2d3748;
      margin-bottom: 24px;
      border-bottom: 2px solid #e2e8f0;
      padding-bottom: 8px;
    }
    .question-feedback {
      background: #ffffff;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      padding: 24px;
      margin-bottom: 16px;
    }
    .q-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    .q-header h4 {
      margin: 0;
      color: #4a5568;
      font-size: 1.1rem;
    }
    .q-score {
      font-weight: 600;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 0.9rem;
    }
    .full-score { background: #c6f6d5; color: #22543d; }
    .zero-score { background: #fed7d7; color: #822727; }
    
    .q-content {
      font-size: 1.1rem;
      color: #1a202c;
      margin-bottom: 24px;
    }
    .response-diff {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    .user-answer strong, .ai-feedback strong {
      display: block;
      color: #4a5568;
      margin-bottom: 8px;
      font-size: 0.9rem;
    }
    .answer-box {
      background: #f7fafc;
      padding: 12px 16px;
      border-radius: 6px;
      border-left: 4px solid #3182ce;
      color: #2d3748;
    }
    .feedback-box {
      background: #ebf8ff;
      padding: 12px 16px;
      border-radius: 6px;
      border-left: 4px solid #38a169;
      color: #234e52;
    }
    .btn-text {
      background: transparent;
      border: none;
      cursor: pointer;
      font-size: 1rem;
    }
    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 400px;
      color: #718096;
    }
    .spinner-large {
      border: 4px solid rgba(49,130,206,0.1);
      width: 48px;
      height: 48px;
      border-radius: 50%;
      border-left-color: #3182ce;
      animation: spin 1s linear infinite;
      margin-bottom: 16px;
    }
    @keyframes spin { 100% { transform: rotate(360deg); } }
  `]
})
export class TestResultsComponent implements OnInit {
  sessionId: string = '';
  result: any = null;
  isLoading = true;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  testType: string = '';

  ngOnInit() {
    this.sessionId = this.route.snapshot.paramMap.get('sessionId') || '';
    this.testType = this.route.snapshot.queryParamMap.get('type') || 'standard';
    if (this.sessionId) {
      this.loadResults();
    }
  }

  get headers() {
    const user = this.authService.currentUser();
    return {
      Authorization: `Bearer ${user?.accessToken || sessionStorage.getItem('access_token')}`
    };
  }

  loadResults() {
    const endpoint = this.testType === 'adaptive'
      ? `${environment.apiUrl}/api/adaptivetests/${this.sessionId}/result`
      : `${environment.apiUrl}/api/candidate/tests/sessions/${this.sessionId}/result`;

    this.http.get<any>(endpoint, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.result = data;
          this.isLoading = false;
        },
        error: (err) => {
          console.error(err);
          alert('Failed to load test results.');
          this.router.navigate(['/candidate/home']);
        }
      });
  }

  goHome() {
    this.router.navigate(['/candidate/home']);
  }

  formatAnswer(answer: string): string {
    if (!answer) return 'No answer provided.';
    try {
      // Try to parse if it's JSON (like for mcq_multi)
      const parsed = JSON.parse(answer);
      if (Array.isArray(parsed)) {
        return parsed.join(', ');
      }
      return answer;
    } catch {
      return answer;
    }
  }
}
