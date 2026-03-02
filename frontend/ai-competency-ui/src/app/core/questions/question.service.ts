import { Injectable, signal } from '@angular/core';
import { of, delay, Observable } from 'rxjs';
import { Question } from '../../models/question.model';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  // Mock Data
  private mockQuestions: Question[] = [
    {
      id: 'q1',
      title: 'What is a Tensor?',
      prompt: 'Define a tensor in the context of deep learning.',
      type: 'short_answer',
      competencyGroupId: 'cg1',
      competencyLevelId: 'cl1',
      difficulty: 2,
      tags: ['basics', 'math'],
      status: 'published',
      updatedAt: new Date()
    },
    {
      id: 'q2',
      title: 'Backpropagation Steps',
      prompt: 'Identify the correct order of operations in backpropagation.',
      type: 'single_choice',
      competencyGroupId: 'cg1',
      competencyLevelId: 'cl2',
      difficulty: 3,
      tags: ['training', 'algorithm'],
      status: 'draft',
      updatedAt: new Date()
    }
  ];

  getQuestions(): Observable<Question[]> {
    return of(this.mockQuestions).pipe(delay(500));
  }

  getQuestion(id: string): Observable<Question | undefined> {
    return of(this.mockQuestions.find(q => q.id === id)).pipe(delay(300));
  }

  createQuestion(question: Omit<Question, 'id' | 'updatedAt'>): Observable<Question> {
    const newQuestion = {
      ...question,
      id: 'q' + (Date.now()),
      updatedAt: new Date()
    };
    this.mockQuestions = [...this.mockQuestions, newQuestion];
    return of(newQuestion).pipe(delay(500));
  }

  updateQuestion(id: string, question: Partial<Question>): Observable<Question> {
    const index = this.mockQuestions.findIndex(q => q.id === id);
    if (index !== -1) {
       this.mockQuestions[index] = { ...this.mockQuestions[index], ...question, updatedAt: new Date() };
       return of(this.mockQuestions[index]).pipe(delay(500));
    }
    throw new Error('Question not found');
  }

  deleteQuestion(id: string): Observable<boolean> {
    this.mockQuestions = this.mockQuestions.filter(q => q.id !== id);
    return of(true).pipe(delay(500));
  }
}
