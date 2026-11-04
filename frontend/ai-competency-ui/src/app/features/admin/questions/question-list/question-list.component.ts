import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { QuestionService } from '../../../../core/questions/question.service';
import { Question, QuestionStatus } from '../../../../models/question.model';
import { COMPETENCY_GROUPS, LEVEL_NAMES, groupName } from '../../../../core/tests/blueprint';
import { apiError } from '../../../../shared/download';

@Component({
  selector: 'app-question-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './question-list.component.html',
  styleUrl: './question-list.component.scss'
})
export class QuestionListComponent {
  private readonly questionService = inject(QuestionService);
  private readonly router = inject(Router);

  readonly groups = COMPETENCY_GROUPS;
  readonly levelOptions = ([1, 2, 3] as const).map(value => ({ value, name: LEVEL_NAMES[value] }));
  readonly groupName = groupName;
  readonly statusBadge: Record<QuestionStatus, string> = { draft: 'secondary', reviewed: 'info', published: 'success', archived: 'warning' };

  questions = signal<Question[]>([]);
  search = signal('');
  status = signal<QuestionStatus | ''>('');
  group = signal('');
  level = signal<number | ''>('');
  reviewing = signal<Question | null>(null);
  reviewNote = '';
  message = signal<string | null>(null);
  error = signal<string | null>(null);

  filtered = computed(() => {
    const text = this.search().trim().toLowerCase();
    return this.questions().filter(q =>
      (!text || q.title.toLowerCase().includes(text) || q.prompt.toLowerCase().includes(text)) &&
      (!this.status() || q.status === this.status()) &&
      (!this.group() || q.groupCode === this.group()) &&
      (!this.level() || q.targetLevel === Number(this.level())));
  });

  awaitingReview = computed(() => this.questions().filter(q => q.status === 'draft' && q.groupCode && q.targetLevel).length);

  constructor() {
    this.load();
  }

  load(): void {
    this.questionService.getQuestions().subscribe({
      next: questions => this.questions.set(questions),
      error: () => this.error.set('Could not load questions.')
    });
  }

  createQuestion() {
    this.router.navigate(['/admin/questions/new']);
  }

  canReview(question: Question): boolean {
    return question.status !== 'archived' && !question.reviewedBy;
  }

  startReview(question: Question): void {
    this.reviewing.set(question);
    this.reviewNote = '';
    this.error.set(null);
  }

  confirmReview(): void {
    const question = this.reviewing();
    if (!question) return;
    this.questionService.reviewQuestion(question.id, this.reviewNote).subscribe({
      next: updated => {
        this.questions.update(list => list.map(q => q.id === updated.id ? { ...q, ...updated } : q));
        this.reviewing.set(null);
        this.message.set(`Reviewed “${updated.title}”. Publish it from the editor to make it available for test forms.`);
      },
      error: (error: HttpErrorResponse) => this.error.set(apiError(error, 'Could not record the review.'))
    });
  }

  publish(question: Question): void {
    this.questionService.updateQuestion(question.id, { ...question, status: 'published' }).subscribe({
      next: updated => {
        this.questions.update(list => list.map(q => q.id === updated.id ? { ...q, ...updated } : q));
        this.message.set(`Published “${updated.title}”.`);
      },
      error: (error: HttpErrorResponse) => this.error.set(apiError(error, 'Could not publish the question.'))
    });
  }

  deleteQuestion(question: Question) {
    if (!confirm(`Delete “${question.title}”? Delivered questions must be archived instead.`)) return;
    this.questionService.deleteQuestion(question.id).subscribe({
      next: () => this.questions.update(list => list.filter(q => q.id !== question.id)),
      error: (error: HttpErrorResponse) => this.error.set(apiError(error, 'Could not delete the question.'))
    });
  }
}
