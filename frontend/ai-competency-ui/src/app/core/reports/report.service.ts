import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AttemptDetail, AttemptRow, ReportSummary } from '../../models/exam.model';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/reports`;

  getSummary(examId: number | null): Observable<ReportSummary> {
    return this.http.get<ReportSummary>(`${this.apiUrl}/summary`, { params: examId ? { examId } : {} });
  }

  getAttempts(examId: number | null): Observable<AttemptRow[]> {
    return this.http.get<AttemptRow[]>(`${this.apiUrl}/attempts`, { params: examId ? { examId } : {} });
  }

  getAttempt(sessionId: string): Observable<AttemptDetail> {
    return this.http.get<AttemptDetail>(`${this.apiUrl}/attempts/${sessionId}`);
  }
}
