import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { RubricService } from '../../../../core/rubrics/rubric.service';
import { DataTableComponent, TableColumn } from '../../../../shared/ui/data-table/data-table.component';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-rubric-list',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './rubric-list.component.html',
  styleUrl: './rubric-list.component.scss'
})
export class RubricListComponent {
   private router = inject(Router);
   private rubricService = inject(RubricService);
   
   rubrics = toSignal(this.rubricService.getRubrics());

   columns: TableColumn[] = [
      { key: 'name', label: 'Name', type: 'text' },
      { key: 'competencyGroupId', label: 'Comp. Group', type: 'text' },
      { key: 'competencyLevelId', label: 'Level', type: 'text' },
      { key: 'updatedAt', label: 'Last Updated', type: 'date' },
      { key: 'actions', label: '', type: 'actions' }
   ];

   createRubric() {
     this.router.navigate(['/admin/rubrics/new']);
   }

   editRubric(rubric: any) {
     this.router.navigate(['/admin/rubrics', rubric.id, 'edit']);
   }
}
