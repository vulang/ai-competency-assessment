import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TestService } from '../../../core/tests/test.service';
import { blueprintSummary } from '../../../core/tests/blueprint';
import { TestBlueprint } from '../../../models/test.model';

@Component({
  selector: 'app-test-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './test-list.component.html',
  styleUrl: './test-list.component.scss'
})
export class TestListComponent {
  private readonly testService = inject(TestService);

  readonly summary = blueprintSummary;
  tests = signal<TestBlueprint[]>([]);
  error = signal<string | null>(null);

  constructor() {
    this.testService.getTests().subscribe({
      next: tests => this.tests.set(tests),
      error: () => this.error.set('Could not load tests.')
    });
  }
}
