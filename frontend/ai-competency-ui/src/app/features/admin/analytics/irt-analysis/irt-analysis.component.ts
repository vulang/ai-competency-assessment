import { Component, OnInit, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CompetencyService, IrtAdminStats, IrtItemStat } from '../../../../core/competency/competency.service';
import { Chart, BarController, CategoryScale, LinearScale, BarElement, Tooltip, Legend, ScatterController, PointElement } from 'chart.js';

Chart.register(BarController, CategoryScale, LinearScale, BarElement, Tooltip, Legend, ScatterController, PointElement);

@Component({
  selector: 'app-irt-analysis',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './irt-analysis.component.html',
  styleUrls: ['./irt-analysis.component.scss'],
})
export class IrtAnalysisComponent implements OnInit {
  @ViewChild('difficultyCanvas') difficultyCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('scatterCanvas') scatterCanvas!: ElementRef<HTMLCanvasElement>;

  stats = signal<IrtAdminStats | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  sortColumn = signal<keyof IrtItemStat>('bParam');
  sortAsc = signal(true);

  private diffChart: Chart | null = null;
  private scatterChart: Chart | null = null;

  constructor(private competency: CompetencyService) {}

  ngOnInit(): void {
    this.competency.getIrtStats().subscribe({
      next: s => {
        this.stats.set(s);
        this.loading.set(false);
        setTimeout(() => {
          this.renderDifficultyHistogram(s.items);
          this.renderScatter(s.items);
        }, 50);
      },
      error: () => {
        this.error.set('Failed to load IRT statistics.');
        this.loading.set(false);
      }
    });
  }

  get sortedItems(): IrtItemStat[] {
    const items = this.stats()?.items ?? [];
    const col = this.sortColumn();
    return [...items].sort((a, b) => {
      const av = a[col] ?? 0, bv = b[col] ?? 0;
      return this.sortAsc() ? (av > bv ? 1 : -1) : (av < bv ? 1 : -1);
    });
  }

  sortBy(col: keyof IrtItemStat): void {
    if (this.sortColumn() === col) {
      this.sortAsc.set(!this.sortAsc());
    } else {
      this.sortColumn.set(col);
      this.sortAsc.set(true);
    }
    // Re-sort is reactive via sortedItems getter
  }

  private renderDifficultyHistogram(items: IrtItemStat[]): void {
    if (!this.difficultyCanvas || items.length === 0) return;
    if (this.diffChart) { this.diffChart.destroy(); }

    const bins = [-4, -3, -2, -1, 0, 1, 2, 3, 4];
    const counts = new Array(bins.length - 1).fill(0);
    items.forEach(it => {
      const b = it.bParam;
      for (let i = 0; i < bins.length - 1; i++) {
        if (b >= bins[i] && b < bins[i + 1]) { counts[i]++; break; }
      }
    });
    const labels = bins.slice(0, -1).map((b, i) => `[${b},${bins[i+1]})`);

    this.diffChart = new Chart(this.difficultyCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Item Count',
          data: counts,
          backgroundColor: 'rgba(99,102,241,0.7)',
          borderColor: 'rgba(99,102,241,1)',
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false },
          tooltip: { callbacks: { label: ctx => `${ctx.parsed.y} items` } } },
        scales: {
          x: { ticks: { color: '#9ca3af' }, grid: { color: 'rgba(255,255,255,0.05)' } },
          y: { ticks: { color: '#9ca3af', stepSize: 1 }, grid: { color: 'rgba(255,255,255,0.05)' } }
        }
      }
    });
  }

  private renderScatter(items: IrtItemStat[]): void {
    if (!this.scatterCanvas || items.length === 0) return;
    if (this.scatterChart) { this.scatterChart.destroy(); }

    const calibrated = items.filter(i => i.isCalibrated).map(i => ({ x: i.bParam, y: i.aParam }));
    const defaults   = items.filter(i => !i.isCalibrated).map(i => ({ x: i.bParam, y: i.aParam }));

    this.scatterChart = new Chart(this.scatterCanvas.nativeElement, {
      type: 'scatter',
      data: {
        datasets: [
          { label: 'Calibrated', data: calibrated, backgroundColor: 'rgba(16,185,129,0.7)', pointRadius: 5 },
          { label: 'Default',    data: defaults,   backgroundColor: 'rgba(245,158,11,0.6)', pointRadius: 4 }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { labels: { color: '#e5e7eb' } } },
        scales: {
          x: { title: { display: true, text: 'b (Difficulty)', color: '#9ca3af' },
               ticks: { color: '#9ca3af' }, grid: { color: 'rgba(255,255,255,0.05)' } },
          y: { title: { display: true, text: 'a (Discrimination)', color: '#9ca3af' },
               ticks: { color: '#9ca3af' }, grid: { color: 'rgba(255,255,255,0.05)' } }
        }
      }
    });
  }
}
