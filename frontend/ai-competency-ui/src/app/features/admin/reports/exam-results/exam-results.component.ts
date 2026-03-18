import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService } from '../../../../core/reports/report.service';
import { DataTableComponent, TableColumn } from '../../../../shared/ui/data-table/data-table.component';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-exam-results',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './exam-results.component.html',
  styleUrl: './exam-results.component.scss'
})
export class ExamResultsComponent {
  private reportService = inject(ReportService);
  
  attempts = toSignal(this.reportService.getExamAttempts());

  columns: TableColumn[] = [
    { key: 'candidateName', label: 'Candidate', type: 'text' },
    { key: 'testName', label: 'Test', type: 'text' },
    { key: 'score', label: 'Score', type: 'text' },
    { 
        key: 'status', 
        label: 'Result', 
        type: 'badge', 
        badgeVariants: { 'passed': 'success', 'failed': 'danger' } 
    },
    { key: 'completedAt', label: 'Date', type: 'date' },
    { key: 'actions', label: '', type: 'actions' }
  ];

  viewDetails(attempt: any) {
    alert('Detail view not implemented yet for: ' + attempt.candidateName);
  }
}
