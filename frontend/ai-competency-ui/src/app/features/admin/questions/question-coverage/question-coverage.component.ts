import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { QuestionService } from '../../../../core/questions/question.service';
import { BankCoverage, CoverageCell } from '../../../../models/question.model';
import { COMPETENCY_GROUPS, LEVEL_NAMES, STANDARD_FORM } from '../../../../core/tests/blueprint';

interface CoverageRow {
  code: string;
  name: string;
  cells: CoverageCell[];
  eligible: number;
  total: number;
}

@Component({
  selector: 'app-question-coverage',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './question-coverage.component.html'
})
export class QuestionCoverageComponent {
  private readonly questionService = inject(QuestionService);

  readonly levels = [1, 2, 3] as const;
  readonly levelNames = LEVEL_NAMES;
  readonly standard = STANDARD_FORM;
  coverage = signal<BankCoverage | null>(null);
  error = signal<string | null>(null);

  rows = computed<CoverageRow[]>(() => {
    const cells = this.coverage()?.cells ?? [];
    return COMPETENCY_GROUPS.map(g => {
      const groupCells = this.levels.map(l => cells.find(c => c.groupCode === g.code && c.level === l)!).filter(Boolean);
      return {
        code: g.code,
        name: g.name,
        cells: groupCells,
        eligible: groupCells.reduce((sum, c) => sum + c.eligible, 0),
        total: groupCells.reduce((sum, c) => sum + c.total, 0)
      };
    });
  });

  // The standard form draws 6 items per group; a deeper bank lets forms be reassembled.
  shortGroups = computed(() => this.rows().filter(r => r.eligible < STANDARD_FORM.maxPerGroup));

  constructor() {
    this.questionService.getCoverage().subscribe({
      next: coverage => this.coverage.set(coverage),
      error: () => this.error.set('Could not load bank coverage.')
    });
  }

  types(cell: CoverageCell): string {
    return Object.entries(cell.byType).map(([type, count]) => `${type}: ${count}`).join(', ') || 'none';
  }
}
