import { Component } from '@angular/core';

@Component({
  selector: 'app-candidate-dashboard',
  templateUrl: './candidate-dashboard.component.html',
})
export class CandidateDashboardComponent {
  modules = [
    {
      title: 'Prompt Engineering',
      summary: 'Craft precise prompts and evaluate output reliability.',
      level: 'Intermediate',
      duration: '30 min',
    },
    {
      title: 'AI Safety & Governance',
      summary: 'Risk-aware deployment, privacy, and compliance scenarios.',
      level: 'Advanced',
      duration: '45 min',
    },
    {
      title: 'Applied Automation',
      summary: 'Design AI workflows with observability and guardrails.',
      level: 'Intermediate',
      duration: '40 min',
    },
  ];
}
