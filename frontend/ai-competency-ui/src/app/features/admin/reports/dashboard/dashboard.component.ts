import { Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BarController, BarElement, CategoryScale, Chart, LinearScale, Tooltip } from 'chart.js';
import { ReportService } from '../../../../core/reports/report.service';
import { ReportSummary } from '../../../../models/exam.model';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnDestroy {
  private readonly reports = inject(ReportService);

  @ViewChild('distribution') set distributionCanvas(canvas: ElementRef<HTMLCanvasElement> | undefined) {
    if (canvas) this.renderDistribution(canvas.nativeElement);
  }

  summary = signal<ReportSummary | null>(null);
  examId: number | null = null;
  error = signal<string | null>(null);
  private chart: Chart | null = null;

  constructor() {
    this.load();
  }

  load(): void {
    this.reports.getSummary(this.examId).subscribe({
      next: summary => { this.summary.set(summary); this.error.set(null); },
      error: () => this.error.set('Could not load the dashboard.')
    });
  }

  /** Background for a heatmap cell: darker green for a higher average percent. */
  heat(percent: number | null): string {
    if (percent === null) return 'transparent';
    return `rgba(56, 161, 105, ${(0.12 + 0.68 * percent / 100).toFixed(2)})`;
  }

  private renderDistribution(canvas: HTMLCanvasElement): void {
    const summary = this.summary();
    if (!summary) return;
    this.chart?.destroy();
    this.chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: summary.scoreDistribution.map(b => `${b.from}–${b.to}`),
        datasets: [{ label: 'Attempts', data: summary.scoreDistribution.map(b => b.count), backgroundColor: 'rgba(49, 130, 206, 0.7)' }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { title: { display: true, text: 'Score (%)' } },
          y: { beginAtZero: true, ticks: { precision: 0 } }
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }
}
