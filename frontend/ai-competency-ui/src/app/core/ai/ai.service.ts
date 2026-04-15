import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

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
  private apiUrl = environment.apiUrl || 'http://localhost:5000';

  generateQuestions(params: GenerationParams): Observable<any[]> {
    const mixItem: MixItem = {
      group: params.competencyGroupId,
      level: params.competencyLevelId,
      type: 'mcq_single',
      difficulty: [2],
      topic: params.topic,
      count: params.count
    };
    return this.generateFromPlan({ total: params.count, mix: [mixItem] });
  }

  generateFromPlan(plan: GenerationPlan): Observable<any[]> {
    return this.generateFromPlanRaw(plan).pipe(
      map(response => this.mapResponseToQuestions(response))
    );
  }

  generateFromPlanRaw(plan: GenerationPlan): Observable<any[]> {
    return this.http.post<any[]>(`${this.apiUrl}/api/question-generation/generate-from-plan`, plan);
  }

  generateAgentic(plan: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/question-generation/generate-agentic`, plan);
  }

  saveQuestions(questions: any[]): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/Questions/batch`, questions);
  }

  private mapResponseToQuestions(rawQuestions: any[]): any[] {
    return rawQuestions.map(q => ({
      id: q.id || `gen-${Date.now()}`,
      title: q.topic || 'AI Generated Question',
      prompt: q.stem || q.question || 'No prompt content',
      type: 'single_choice',
      competencyGroupId: q.group,
      competencyLevelId: q.level,
      difficulty: q.difficulty,
      tags: q.tags || [],
      status: 'draft',
      updatedAt: new Date()
    }));
  }
}
