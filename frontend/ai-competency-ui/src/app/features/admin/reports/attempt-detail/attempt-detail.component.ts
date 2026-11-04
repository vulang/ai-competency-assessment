import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReportService } from '../../../../core/reports/report.service';
import { LEVEL_NAMES, groupName } from '../../../../core/tests/blueprint';
import { AttemptDetail } from '../../../../models/exam.model';

@Component({
  selector: 'app-attempt-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './attempt-detail.component.html'
})
export class AttemptDetailComponent {
  private readonly reports = inject(ReportService);

  readonly levelNames = LEVEL_NAMES;
  readonly groupName = groupName;
  attempt = signal<AttemptDetail | null>(null);
  error = signal<string | null>(null);

  constructor() {
    this.reports.getAttempt(inject(ActivatedRoute).snapshot.paramMap.get('sessionId')!).subscribe({
      next: attempt => this.attempt.set(attempt),
      error: () => this.error.set('Could not load this attempt.')
    });
  }
}
