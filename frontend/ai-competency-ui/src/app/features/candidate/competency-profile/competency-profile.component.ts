import { Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CompetencyService } from '../../../core/competency/competency.service';
import { GroupScore, TestProfile } from '../../../models/test.model';
import { LEVEL_NAMES } from '../../../core/tests/blueprint';
import { Chart, RadarController, RadialLinearScale, PointElement, LineElement, Filler, Tooltip, Legend } from 'chart.js';

Chart.register(RadarController, RadialLinearScale, PointElement, LineElement, Filler, Tooltip, Legend);

@Component({
  selector: 'app-competency-profile',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './competency-profile.component.html',
  styleUrls: ['./competency-profile.component.scss'],
})
export class CompetencyProfileComponent implements OnDestroy {
  private readonly competency = inject(CompetencyService);

  @ViewChild('radarCanvas') set radarCanvas(canvas: ElementRef<HTMLCanvasElement> | undefined) {
    const profile = this.profile();
    if (canvas && profile) this.renderChart(canvas.nativeElement, profile);
  }

  profile = signal<TestProfile | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  private chart: Chart | null = null;

  constructor() {
    this.competency.getMyTestProfile().subscribe({
      next: p => { this.profile.set(p); this.loading.set(false); },
      error: err => {
        this.error.set(err.status === 404
          ? 'No competency profile yet. Complete the standard test to see your seven-group profile.'
          : 'Failed to load profile.');
        this.loading.set(false);
      }
    });
  }

  levelLabel(group: GroupScore): string {
    if (group.percent === null) return 'Not measured';
    return group.level ? LEVEL_NAMES[group.level] : 'Level pending';
  }

  private renderChart(canvas: HTMLCanvasElement, p: TestProfile): void {
    this.chart?.destroy();
    this.chart = new Chart(canvas, {
      type: 'radar',
      data: {
        labels: p.groups.map(g => g.groupName),
        datasets: [{
          label: 'Percent correct',
          data: p.groups.map(g => g.percent ?? 0),
          backgroundColor: 'rgba(99, 102, 241, 0.2)',
          borderColor: 'rgba(99, 102, 241, 0.9)',
          borderWidth: 2,
          pointBackgroundColor: 'rgba(99, 102, 241, 1)',
          pointRadius: 4,
        }]
      },
      options: {
        responsive: true,
        scales: {
          r: {
            min: 0, max: 100,
            ticks: { stepSize: 20, color: '#6b7280', font: { size: 10 } },
            grid: { color: 'rgba(255,255,255,0.1)' },
            pointLabels: { color: '#e5e7eb', font: { size: 11 } },
          }
        },
        plugins: { legend: { display: false } }
      }
    });
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }
}
