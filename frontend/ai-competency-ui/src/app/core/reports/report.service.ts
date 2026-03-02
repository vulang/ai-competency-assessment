import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import { ExamAttempt } from '../../models/exam.model';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private mockAttempts: ExamAttempt[] = [
    {
       id: 'ea1',
       candidateName: 'John Doe',
       testName: 'AI Fundamentals',
       score: 85,
       passingScore: 70,
       status: 'passed',
       completedAt: new Date('2023-10-15')
    },
    {
       id: 'ea2',
       candidateName: 'Jane Smith',
       testName: 'Deep Learning Basics',
       score: 65,
       passingScore: 70,
       status: 'failed',
       completedAt: new Date('2023-10-16')
    }
  ];

  getExamAttempts(): Observable<ExamAttempt[]> {
     return of(this.mockAttempts).pipe(delay(500));
  }
}
