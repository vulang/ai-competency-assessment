import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { delay, Observable, of } from 'rxjs';
import { TestBlueprint } from '../../models/test.model';

@Injectable({
  providedIn: 'root'
})
export class TestService {
  private apiUrl = '/api/tests';

  constructor(private http: HttpClient) {}

  getTests(): Observable<TestBlueprint[]> {
    return this.http.get<TestBlueprint[]>(this.apiUrl);
  }

  getTest(id: string): Observable<TestBlueprint | undefined> {
    return this.http.get<TestBlueprint>(`${this.apiUrl}/${id}`);
  }

  createTest(test: Omit<TestBlueprint, 'id' | 'createdAt' | 'updatedAt'>): Observable<TestBlueprint> {
    const createDto = {
      name: test.name,
      description: test.description,
      durationMinutes: test.durationMinutes,
      passingScore: test.passingScore,
      questionCount: test.questionCount,
      status: test.status
    };
    return this.http.post<TestBlueprint>(this.apiUrl, createDto);
  }
}
