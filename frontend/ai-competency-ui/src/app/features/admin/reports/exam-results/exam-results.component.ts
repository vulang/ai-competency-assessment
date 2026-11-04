import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReportService } from '../../../../core/reports/report.service';
import { AttemptRow } from '../../../../models/exam.model';

@Component({
  selector: 'app-exam-results',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './exam-results.component.html',
  styleUrl: './exam-results.component.scss'
})
export class ExamResultsComponent {
  private readonly reportService = inject(ReportService);

  attempts = signal<AttemptRow[]>([]);
  error = signal<string | null>(null);

  constructor() {
    this.reportService.getAttempts(null).subscribe({
      next: attempts => this.attempts.set(attempts),
      error: () => this.error.set('Could not load attempts.')
    });
  }
}
