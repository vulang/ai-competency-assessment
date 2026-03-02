import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TestService } from '../../../core/tests/test.service';
import { DataTableComponent, TableColumn } from '../../../shared/ui/data-table/data-table.component';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-test-list',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './test-list.component.html',
  styleUrl: './test-list.component.scss'
})
export class TestListComponent {
   private router = inject(Router);
   private testService = inject(TestService);
   
   tests = toSignal(this.testService.getTests());

   columns: TableColumn[] = [
      { key: 'name', label: 'Name', type: 'text' },
      { key: 'questionCount', label: 'Questions', type: 'text' },
      { key: 'durationMinutes', label: 'Duration (m)', type: 'text' },
      { 
          key: 'status', 
          label: 'Status', 
          type: 'badge', 
          badgeVariants: { 'active': 'success', 'draft': 'secondary' } 
      },
      { key: 'createdAt', label: 'Created', type: 'date' },
      { key: 'actions', label: '', type: 'actions' }
   ];

   createTest() {
     this.router.navigate(['/admin/tests/new']);
   }

   editTest(test: any) {
     this.router.navigate(['/admin/tests', test.id, 'edit']);
   }
}
