import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { BankCoverage, Question } from '../../models/question.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Questions`;

  getQuestions(): Observable<Question[]> {
    return this.http.get<Question[]>(this.apiUrl);
  }

  getQuestion(id: string): Observable<Question | undefined> {
    return this.http.get<Question>(`${this.apiUrl}/${id}`);
  }

  createQuestion(question: Omit<Question, 'id' | 'updatedAt'>): Observable<Question> {
    return this.http.post<Question>(this.apiUrl, question);
  }

  updateQuestion(id: string, question: Partial<Question>): Observable<Question> {
    return this.http.put<Question>(`${this.apiUrl}/${id}`, question);
  }

  /** Records expert review; only reviewed questions can be published or used in test forms. */
  reviewQuestion(id: string, note: string): Observable<Question> {
    return this.http.post<Question>(`${this.apiUrl}/${id}/review`, { note });
  }

  getCoverage(): Observable<BankCoverage> {
    return this.http.get<BankCoverage>(`${this.apiUrl}/coverage`);
  }

  deleteQuestion(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`).pipe(map(() => true));
  }
}
