import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { QuestionService } from '../../../core/questions/question.service';
import { DataTableComponent, TableColumn } from '../../../shared/ui/data-table/data-table.component';
import { Question } from '../../../models/question.model';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-question-list',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './question-list.component.html',
  styleUrl: './question-list.component.scss'
})
export class QuestionListComponent {
  private router = Router;
  private questionService = inject(QuestionService);
  private _router = inject(Router);

  questions = toSignal(this.questionService.getQuestions());

  columns: TableColumn[] = [
    { key: 'title', label: 'Title', type: 'text' },
    { key: 'type', label: 'Type', type: 'text' },
    { key: 'difficulty', label: 'Difficulty', type: 'text' },
     { 
        key: 'status', 
        label: 'Status', 
        type: 'badge', 
        badgeVariants: { 'published': 'success', 'draft': 'secondary', 'archived': 'warning' } 
    },
    { key: 'updatedAt', label: 'Last Updated', type: 'date' },
    { key: 'actions', label: '', type: 'actions' }
  ];

  createQuestion() {
    this._router.navigate(['/admin/questions/new']);
  }

  editQuestion(question: Question) {
    this._router.navigate(['/admin/questions', question.id, 'edit']);
  }

  deleteQuestion(question: Question) {
      if(confirm('Are you sure you want to delete this question?')) {
          this.questionService.deleteQuestion(question.id).subscribe(() => {
              // Reload - in real app trigger signal update
              alert('Deleted ' + question.title);
          });
      }
  }
}
