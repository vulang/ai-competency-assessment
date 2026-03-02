import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { QuestionService } from '../../../core/questions/question.service';
import { Question, QuestionType } from '../../../models/question.model';
import { switchMap, of } from 'rxjs';

@Component({
  selector: 'app-question-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './question-form.component.html',
  styleUrl: './question-form.component.scss'
})
export class QuestionFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private questionService = inject(QuestionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEditMode = signal(false);
  isSaving = signal(false);
  questionId: string | null = null;

  form = this.fb.group({
    title: ['', Validators.required],
    prompt: ['', Validators.required],
    type: ['single_choice' as QuestionType, Validators.required],
    competencyGroupId: ['', Validators.required],
    competencyLevelId: ['', Validators.required],
    difficulty: [1, [Validators.required, Validators.min(1), Validators.max(5)]],
    status: ['draft', Validators.required]
  });

  ngOnInit() {
    this.route.paramMap.pipe(
      switchMap(params => {
        this.questionId = params.get('id');
        if (this.questionId) {
          this.isEditMode.set(true);
          return this.questionService.getQuestion(this.questionId);
        }
        return of(null);
      })
    ).subscribe(question => {
      if (question) {
        this.form.patchValue(question);
      }
    });
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.isSaving.set(true);
    const formValue = this.form.value as any; // Cast for simplicity

    if (this.isEditMode() && this.questionId) {
       this.questionService.updateQuestion(this.questionId, formValue).subscribe(() => {
          this.router.navigate(['/admin/questions']);
       });
    } else {
       this.questionService.createQuestion(formValue).subscribe(() => {
          this.router.navigate(['/admin/questions']);
       });
    }
  }
}
