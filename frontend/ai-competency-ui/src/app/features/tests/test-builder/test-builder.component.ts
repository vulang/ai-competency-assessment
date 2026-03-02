import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TestService } from '../../../core/tests/test.service';

@Component({
  selector: 'app-test-builder',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './test-builder.component.html',
  styleUrl: './test-builder.component.scss'
})
export class TestBuilderComponent {
   private fb = inject(FormBuilder);
   private testService = inject(TestService);
   private router = inject(Router);

   form = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      durationMinutes: [60, [Validators.required, Validators.min(1)]],
      passingScore: [70, [Validators.required, Validators.min(1), Validators.max(100)]],
      status: ['draft', Validators.required],
      questionCount: [0] // handled later by rules
   });

   save() {
      if(this.form.invalid) return;

      const val = this.form.value as any;
      this.testService.createTest(val).subscribe(() => {
          this.router.navigate(['/admin/tests']);
      });
   }
}
