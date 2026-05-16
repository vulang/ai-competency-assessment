import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CompetencyService, CompetencyProfile, SkillMastery } from '../../../core/competency/competency.service';
import { Chart, RadarController, RadialLinearScale, PointElement, LineElement, Filler, Tooltip, Legend } from 'chart.js';

Chart.register(RadarController, RadialLinearScale, PointElement, LineElement, Filler, Tooltip, Legend);

const DOMAIN_LABELS: Record<string, string> = {
  ai_fundamentals:   'AI Fundamentals',
  data:              'Data',
  critical_thinking: 'Critical Thinking',
  ai_use_cases:      'AI Use Cases',
  ai_ethics:         'AI Ethics',
  ai_tools:          'AI Tools',
  future_of_work:    'Future of Work',
};

@Component({
  selector: 'app-competency-profile',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './competency-profile.component.html',
  styleUrls: ['./competency-profile.component.scss'],
})
export class CompetencyProfileComponent implements OnInit, AfterViewInit {
  @ViewChild('radarCanvas') radarCanvas!: ElementRef<HTMLCanvasElement>;

  profile = signal<CompetencyProfile | null>(null);
  skillMasteries = signal<SkillMastery[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  private chart: Chart | null = null;

  constructor(private competency: CompetencyService) {}

  ngOnInit(): void {
    this.competency.getMyProfile().subscribe({
      next: p => {
        this.profile.set(p);
        this.loading.set(false);
        setTimeout(() => this.renderChart(p), 50);
      },
      error: err => {
        this.error.set(err.status === 404
          ? 'No competency profile yet. Complete a test to generate your profile.'
          : 'Failed to load profile.');
        this.loading.set(false);
      }
    });

    this.competency.getMySkillMastery().subscribe({
      next: m => this.skillMasteries.set(m),
      error: () => {} // non-critical
    });
  }

  ngAfterViewInit(): void {
    const p = this.profile();
    if (p) this.renderChart(p);
  }

  get domainEntries(): { label: string; score: number }[] {
    const p = this.profile();
    if (!p) return [];
    return Object.entries(p.domainScores).map(([key, score]) => ({
      label: DOMAIN_LABELS[key] ?? key,
      score,
    }));
  }

  get levelColor(): string {
    switch (this.profile()?.overallLevel) {
      case 'Create': return '#10b981';
      case 'Apply':  return '#3b82f6';
      default:       return '#f59e0b';
    }
  }

  get thetaDisplay(): string {
    const t = this.profile()?.theta ?? 0;
    return `${t >= 0 ? '+' : ''}${t.toFixed(2)}`;
  }

  private renderChart(p: CompetencyProfile): void {
    if (!this.radarCanvas) return;
    if (this.chart) { this.chart.destroy(); this.chart = null; }

    const labels = Object.keys(p.domainScores).map(k => DOMAIN_LABELS[k] ?? k);
    const data   = Object.values(p.domainScores);

    this.chart = new Chart(this.radarCanvas.nativeElement, {
      type: 'radar',
      data: {
        labels,
        datasets: [{
          label: 'Competency Score',
          data,
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
}
