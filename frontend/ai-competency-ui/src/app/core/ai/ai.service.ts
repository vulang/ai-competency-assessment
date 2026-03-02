import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { delay, map, Observable, of } from 'rxjs';
import { Question } from '../../models/question.model';

export interface MixItem {
  group: string;
  level: string;
  type: string;
  difficulty: number[];
  topic: string;
  count: number;
}

export interface GenerationPlan {
  total: number;
  mix: MixItem[];
}

export interface GenerationParams {
  competencyGroupId: string;
  competencyLevelId: string;
  count: number;
  types: string[];
  topic: string;
  language: 'en' | 'vn';
}

@Injectable({
  providedIn: 'root'
})
export class AiService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/question-generation'; // Should be in environment

  generateQuestions(params: GenerationParams): Observable<Question[]> {
    // Legacy support or adapt to plan
    const mixItem: MixItem = {
      group: params.competencyGroupId,
      level: params.competencyLevelId,
      type: 'mcq_single', // Default or map from params.types
      difficulty: [2],
      topic: params.topic,
      count: params.count
    };
    return this.generateFromPlan({ total: params.count, mix: [mixItem] });
  }

  generateFromPlan(plan: GenerationPlan): Observable<Question[]> {
    return this.generateFromPlanRaw(plan).pipe(
      map(response => this.mapResponseToQuestions(response))
    );
  }

  generateFromPlanRaw(plan: GenerationPlan): Observable<any[]> {
    return this.http.post<any[]>(`${this.apiUrl}/generate-from-plan`, plan);
  }

  private mapResponseToQuestions(rawQuestions: any[]): Question[] {
    return rawQuestions.map(q => ({
      id: q.id || `gen-${Date.now()}`,
      title: q.topic || 'AI Generated Question',
      prompt: q.stem || q.question || 'No prompt content',
      type: 'single_choice', // Map q.type to QuestionType
      competencyGroupId: q.group,
      competencyLevelId: q.level,
      difficulty: q.difficulty,
      tags: q.tags || [],
      status: 'draft',
      updatedAt: new Date()
    }));
  }
}
