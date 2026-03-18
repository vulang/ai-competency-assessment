import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-test-runner',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './test-runner.component.html',
  styleUrl: './test-runner.component.css'
})
export class TestRunnerComponent implements OnInit {
  sessionId: string = '';
  session: any = null;
  isLoading = true;
  isSubmitting = false;
  
  currentIndex = 0;
  responses: { [questionId: number]: any } = {};

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  ngOnInit() {
    this.sessionId = this.route.snapshot.paramMap.get('sessionId') || '';
    if (this.sessionId) {
      this.loadSession();
    }
  }

  get headers() {
    const user = this.authService.currentUser();
    return {
      Authorization: `Bearer ${user?.accessToken || sessionStorage.getItem('access_token')}`
    };
  }

  loadSession() {
    this.http.get<any>(`${environment.apiUrl}/api/candidate/tests/sessions/${this.sessionId}`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.session = data;
          this.isLoading = false;
          // Initialize responses
          this.session.questions.forEach((q: any) => {
            if (q.type === 'mcq_multi') {
              this.responses[q.id] = [];
            } else {
              this.responses[q.id] = '';
            }
          });
        },
        error: (err) => {
          console.error(err);
          alert('Failed to load test session. You might not have access or it might have been completed.');
          this.router.navigate(['/candidate/home']);
        }
      });
  }

  get currentQuestion() {
    return this.session?.questions[this.currentIndex];
  }

  previousQuestion() {
    if (this.currentIndex > 0) {
      this.currentIndex--;
    }
  }

  nextQuestion() {
    if (this.currentIndex < this.session.questions.length - 1) {
      this.currentIndex++;
    }
  }

  isMultiSelected(questionId: number, option: string): boolean {
    const answers = this.responses[questionId] as string[];
    return answers && answers.includes(option);
  }

  toggleMultiSelection(questionId: number, option: string) {
    const answers = this.responses[questionId] as string[];
    const index = answers.indexOf(option);
    if (index > -1) {
      answers.splice(index, 1);
    } else {
      answers.push(option);
    }
  }

  submitTest() {
    if (confirm('Are you sure you want to submit your assessment? You cannot change your answers after submission.')) {
      this.isSubmitting = true;
      
      const payload = {
        responses: this.session.questions.map((q: any) => {
          let answerStr = '';
          if (q.type === 'mcq_multi') {
            answerStr = JSON.stringify(this.responses[q.id] || []);
          } else {
            answerStr = this.responses[q.id] || '';
          }
          return {
            questionId: q.id,
            answer: answerStr
          };
        })
      };

      this.http.post<any>(`${environment.apiUrl}/api/candidate/tests/sessions/${this.sessionId}/submit`, payload, { headers: this.headers })
        .subscribe({
          next: () => {
            this.router.navigate(['/candidate/results', this.sessionId]);
          },
          error: (err) => {
            console.error(err);
            alert('Failed to submit test.');
            this.isSubmitting = false;
          }
        });
    }
  }
}
