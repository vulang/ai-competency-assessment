import { Injectable } from '@angular/core';
import { delay, Observable, of } from 'rxjs';
import { Rubric } from '../../models/rubric.model';

@Injectable({
  providedIn: 'root'
})
export class RubricService {
  private mockRubrics: Rubric[] = [
    {
      id: 'r1',
      name: 'Coding Style Rubric',
      competencyGroupId: 'cg1',
      competencyLevelId: 'cl2',
      criteria: [
        { id: 'c1', description: 'Follows naming conventions', maxScore: 5 },
        { id: 'c2', description: 'Proper indentation', maxScore: 5 }
      ],
      updatedAt: new Date()
    }
  ];

  getRubrics(): Observable<Rubric[]> {
    return of(this.mockRubrics).pipe(delay(500));
  }

  createRubric(rubric: Omit<Rubric, 'id' | 'updatedAt'>): Observable<Rubric> {
    const newRubric = { ...rubric, id: 'r' + Date.now(), updatedAt: new Date() };
    this.mockRubrics = [...this.mockRubrics, newRubric];
    return of(newRubric).pipe(delay(500));
  }
}
