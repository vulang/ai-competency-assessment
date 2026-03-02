import { Injectable } from '@angular/core';
import { delay, Observable, of } from 'rxjs';
import { TestBlueprint } from '../../models/test.model';

@Injectable({
  providedIn: 'root'
})
export class TestService {
  private mockTests: TestBlueprint[] = [
    {
       id: 'tb1',
       name: 'AI Engineering Baseline Exam',
       description: 'Standard baseline assessment for new hires.',
       durationMinutes: 60,
       passingScore: 70,
       questionCount: 30,
       status: 'active',
       createdAt: new Date(),
       updatedAt: new Date()
    }
  ];

  getTests(): Observable<TestBlueprint[]> {
    return of(this.mockTests).pipe(delay(500));
  }

  getTest(id: string): Observable<TestBlueprint | undefined> {
    return of(this.mockTests.find(t => t.id === id)).pipe(delay(300));
  }

  createTest(test: Omit<TestBlueprint, 'id' | 'createdAt' | 'updatedAt'>): Observable<TestBlueprint> {
     const newTest: TestBlueprint = {
       ...test,
       id: 'tb' + Date.now(),
       createdAt: new Date(),
       updatedAt: new Date()
     };
     this.mockTests = [...this.mockTests, newTest];
     return of(newTest).pipe(delay(500));
  }
}
