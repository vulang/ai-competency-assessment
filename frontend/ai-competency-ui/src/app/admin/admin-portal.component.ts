import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-portal',
  templateUrl: './admin-portal.component.html',
})
export class AdminPortalComponent {
  stats = [
    { label: 'Active exams', value: 12 },
    { label: 'Candidates today', value: 84 },
    { label: 'Pending grading', value: 6 },
  ];

  pipelines = [
    { name: 'Sandbox grading', status: 'Running', latency: '42s' },
    { name: 'LLM judge queue', status: 'Healthy', latency: '11s' },
    { name: 'Analytics rollup', status: 'Scheduled', latency: '2m' },
  ];
}
