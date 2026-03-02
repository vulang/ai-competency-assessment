import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { RubricService } from '../../../core/rubrics/rubric.service';

@Component({
  selector: 'app-rubric-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './rubric-form.component.html',
  styleUrl: './rubric-form.component.scss'
})
export class RubricFormComponent {
  private fb = inject(FormBuilder);
  private rubricService = inject(RubricService);
  private router = inject(Router);

  form = this.fb.group({
     name: ['', Validators.required],
     competencyGroupId: ['', Validators.required],
     competencyLevelId: ['', Validators.required],
     criteria: this.fb.array([])
  });

  get criteria() {
    return this.form.get('criteria') as FormArray;
  }

  addCriteria() {
    this.criteria.push(this.fb.group({
       id: ['c' + Date.now()],
       description: ['', Validators.required],
       maxScore: [5, Validators.required]
    }));
  }

  removeCriteria(index: number) {
    this.criteria.removeAt(index);
  }

  onSubmit() {
    if(this.form.invalid) return;

    this.rubricService.createRubric(this.form.value as any).subscribe(() => {
       this.router.navigate(['/admin/rubrics']);
    });
  }
}
