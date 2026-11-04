import { Component, ElementRef, OnDestroy, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { BarController, BarElement, CategoryScale, Chart, LinearScale, Tooltip } from 'chart.js';
import { TestService } from '../../../core/tests/test.service';
import { LEVEL_NAMES, groupName } from '../../../core/tests/blueprint';
import { CttItemResult, ItemAnalysisExport, ItemAnalysisItem, ItemAnalysisReport } from '../../../models/test.model';
import { apiError, saveBlob } from '../../../shared/download';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip);

/** Below this many respondents, CTT estimates are reported as exploratory. */
export const EXPLORATORY_SAMPLE = 30;

export const FLAG_LABELS: Record<string, string> = {
  zero_variance: 'No variance',
  too_hard: 'Too hard',
  too_easy: 'Too easy',
  low_discrimination: 'Low discrimination',
  negative_discrimination: 'Negative discrimination',
  alpha_increases_if_deleted: 'α rises if deleted',
  nonfunctional_distractor: 'Unused distractor',
  distractor_attracts_high_scorers: 'Distractor attracts high scorers'
};

interface ItemRow { meta: ItemAnalysisItem; stats: CttItemResult }

@Component({
  selector: 'app-item-analysis',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './item-analysis.component.html'
})
export class ItemAnalysisComponent implements OnDestroy {
  private readonly testService = inject(TestService);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  @ViewChild('histogram') set histogramCanvas(canvas: ElementRef<HTMLCanvasElement> | undefined) {
    if (canvas) this.renderHistogram(canvas.nativeElement);
  }

  readonly flagLabels = FLAG_LABELS;
  readonly levelNames = LEVEL_NAMES;
  readonly groupName = groupName;
  readonly exploratory = EXPLORATORY_SAMPLE;
  report = signal<ItemAnalysisReport | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  flaggedOnly = signal(false);
  expanded = signal<string | null>(null);
  private chart: Chart | null = null;

  rows = computed<ItemRow[]>(() => {
    const report = this.report();
    if (!report) return [];
    const meta = new Map(report.items.map(i => [String(i.questionId), i]));
    return report.analysis.items
      .map(stats => ({ meta: meta.get(stats.itemId)!, stats }))
      .filter(row => !this.flaggedOnly() || row.stats.flags.length > 0);
  });

  flaggedCount = computed(() => this.report()?.analysis.items.filter(i => i.flags.length).length ?? 0);

  constructor() {
    this.testService.itemAnalysis(this.id).subscribe({
      next: report => { this.report.set(report); this.loading.set(false); },
      error: (error: HttpErrorResponse) => { this.error.set(apiError(error, 'Could not compute the item analysis.')); this.loading.set(false); }
    });
  }

  optionLabel(index: number): string {
    return String.fromCharCode(65 + index);
  }

  toggle(itemId: string): void {
    this.expanded.update(current => current === itemId ? null : itemId);
  }

  download(kind: ItemAnalysisExport): void {
    this.testService.exportItemAnalysis(this.id, kind).subscribe({
      next: blob => saveBlob(blob, `test-${this.id}-${kind}.csv`),
      error: (error: HttpErrorResponse) => this.error.set(apiError(error, 'Could not export the analysis.'))
    });
  }

  private renderHistogram(canvas: HTMLCanvasElement): void {
    const report = this.report();
    if (!report) return;
    this.chart?.destroy();
    const bins = report.analysis.total.histogram;
    this.chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: bins.map(b => String(b.score)),
        datasets: [{ label: 'Respondents', data: bins.map(b => b.count), backgroundColor: 'rgba(49, 130, 206, 0.7)' }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { title: { display: true, text: 'Total score' } },
          y: { beginAtZero: true, ticks: { precision: 0 }, title: { display: true, text: 'Respondents' } }
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }
}
