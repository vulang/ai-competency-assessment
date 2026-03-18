import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-adaptive-runner',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './adaptive-runner.component.html',
  styleUrl: './adaptive-runner.component.css'
})
export class AdaptiveRunnerComponent implements OnInit {
  sessionId: string = '';
  questionData: any = null;
  isLoading = true;
  isSubmitting = false;
  
  response: string = '';

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  ngOnInit() {
    this.sessionId = this.route.snapshot.paramMap.get('sessionId') || '';
    if (this.sessionId) {
      this.loadNextQuestion();
    }
  }

  get headers() {
    const user = this.authService.currentUser();
    return {
      Authorization: `Bearer ${user?.accessToken || sessionStorage.getItem('access_token')}`
    };
  }

  get currentQuestion() {
    return this.questionData?.question;
  }

  get hasAnswered() {
      if (Array.isArray(this.response)) {
          return this.response.length > 0;
      }
      return this.response !== null && this.response.trim() !== '';
  }

  loadNextQuestion() {
    this.isLoading = true;
    this.response = ''; // Reset input
    
    this.http.get<any>(`${environment.apiUrl}/api/adaptivetests/${this.sessionId}/next`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.questionData = data;
          this.isLoading = false;
          
          if (this.currentQuestion?.type === 'mcq_multi') {
            this.response = [] as any;
          } else {
            this.response = '';
          }
        },
        error: (err) => {
          console.error(err);
          // 400 with Status Completed means we reached the end
          if (err.error?.status === 'Completed') {
              this.router.navigate(['/candidate/results', this.sessionId], { queryParams: { type: 'adaptive' } });
          } else {
             alert('Failed to load next test question.');
             this.router.navigate(['/candidate/home']);
          }
        }
      });
  }

  isMultiSelected(option: string): boolean {
    const answers = this.response as unknown as string[];
    return answers && Array.isArray(answers) && answers.includes(option);
  }

  toggleMultiSelection(option: string) {
    if (!Array.isArray(this.response)) {
        this.response = [] as any;
    }
    const answers = this.response as unknown as string[];
    const index = answers.indexOf(option);
    if (index > -1) {
      answers.splice(index, 1);
    } else {
      answers.push(option);
    }
    // Just to trigger change detection if needed, though pushing to array might be enough
    this.response = answers as any;
  }

  submitAnswer() {
    if (!this.hasAnswered) return;
    
    this.isSubmitting = true;
      
    let answerStr = '';
    if (this.currentQuestion?.type === 'mcq_multi') {
      answerStr = JSON.stringify(this.response || []);
    } else {
      answerStr = this.response || '';
    }

    const payload = {
        questionId: this.currentQuestion.id,
        answer: answerStr
    };

    this.http.post<any>(`${environment.apiUrl}/api/adaptivetests/${this.sessionId}/submit`, payload, { headers: this.headers })
      .subscribe({
        next: (result) => {
            this.isSubmitting = false;
            if (result.isTestComplete) {
                this.router.navigate(['/candidate/results', this.sessionId], { queryParams: { type: 'adaptive' } });
            } else {
                // Fetch next question based on new ability
                this.loadNextQuestion();
            }
        },
        error: (err) => {
            console.error(err);
            alert('Failed to submit answer.');
            this.isSubmitting = false;
        }
      });
  }
}
