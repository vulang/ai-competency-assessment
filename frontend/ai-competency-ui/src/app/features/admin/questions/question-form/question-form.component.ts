import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { QuestionService } from '../../../../core/questions/question.service';
import { Question, QuestionType, QuestionStatus } from '../../../../models/question.model';
import { FrameworkService, FrameworkDomain } from '../../../../core/competency/framework.service';
import { HttpErrorResponse } from '@angular/common/http';
import { switchMap, of, forkJoin } from 'rxjs';

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
  private frameworkService = inject(FrameworkService);
  domains = signal<FrameworkDomain[]>([]);
  errorMessage = signal<string | null>(null);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEditMode = signal(false);
  isSaving = signal(false);
  questionId: string | null = null;

  form = this.fb.nonNullable.group({
    title: [''],
    prompt: ['', Validators.required],
    type: this.fb.nonNullable.control<QuestionType>('mcq_single', Validators.required),
    competencyGroupId: ['', Validators.required],
    competencyLevelId: ['', Validators.required],
    status: this.fb.nonNullable.control<QuestionStatus>('draft', Validators.required),
    skillId: this.fb.control<number | null>(null, Validators.required),
    difficulty: this.fb.control<Question['difficulty']>(null, [Validators.min(1), Validators.max(5)]),
    tags: this.fb.nonNullable.control<string[]>([]),
    options: this.fb.nonNullable.array<string>([]),
    answer: [''],
    context: [''],
    responseStructure: this.fb.nonNullable.array<string>([]),
    rubric: this.fb.nonNullable.array<string>([]),
    judgePromptHints: [''],
    behaviorsAddressed: this.fb.nonNullable.array<string>([])
  });

  ngOnInit() {
    this.route.paramMap.pipe(
      switchMap(params => {
        this.questionId = params.get('id');
        if (this.questionId) {
          this.isEditMode.set(true);
          return forkJoin({ framework: this.frameworkService.getActive(), question: this.questionService.getQuestion(this.questionId) });
        }
        return forkJoin({ framework: this.frameworkService.getActive(), question: of(null) });
      })
    ).subscribe({ next: ({ framework, question }) => {
      this.domains.set(framework.domains);
      if (question) {
        this.form.patchValue({
          title: question.title,
          prompt: question.prompt,
          type: question.type,
          competencyGroupId: question.competencyGroupId,
          competencyLevelId: question.competencyLevelId,
          status: question.status,
          answer: Array.isArray(question.answer) ? JSON.stringify(question.answer) : question.answer || '',
          skillId: question.skillId ?? null,
          difficulty: question.difficulty,
          tags: question.tags,
          context: question.context,
          judgePromptHints: question.judgePromptHints
        });
        
        ['options', 'responseStructure', 'rubric', 'behaviorsAddressed'].forEach(arrName => {
          const dataArray = question[arrName as keyof Question] as string[] | undefined;
          if (dataArray && Array.isArray(dataArray)) {
            const arr = this.form.get(arrName) as FormArray;
            arr.clear();
            dataArray.forEach(item => arr.push(this.fb.nonNullable.control(item)));
          }
        });
      }
    }, error: () => this.errorMessage.set('Could not load the question or competency framework. Reload to try again.') });
  }

  get skills() {
    return this.domains().find(d => d.name === this.form.controls.competencyGroupId.value)?.skills ?? [];
  }

  changeGroup(): void {
    this.form.controls.skillId.setValue(this.skills.find(s => s.isDefault)?.id ?? null);
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.isSaving.set(true);
    this.errorMessage.set(null);
    const formValue = this.form.getRawValue();
    const save = this.isEditMode() && this.questionId
      ? this.questionService.updateQuestion(this.questionId, formValue)
      : this.questionService.createQuestion(formValue);
    save.subscribe({
      next: () => { this.isSaving.set(false); this.router.navigate(['/admin/questions']); },
      error: (error: HttpErrorResponse) => {
        this.isSaving.set(false);
        const validation = error.error?.errors as Record<string, string[]> | undefined;
        this.errorMessage.set(validation ? Object.values(validation).flat().join(' ') : 'Could not save the question. Please try again.');
      }
    });
  }

  getArrayControls(name: string) {
    return (this.form.get(name) as FormArray).controls;
  }

  addArrayItem(name: string, val: string = '') {
    (this.form.get(name) as FormArray).push(this.fb.nonNullable.control(val));
  }

  removeArrayItem(name: string, index: number) {
    (this.form.get(name) as FormArray).removeAt(index);
  }

  get optionsControls() { return this.getArrayControls('options'); }
  addOption(val: string = '') { this.addArrayItem('options', val); }
  removeOption(index: number) { this.removeArrayItem('options', index); }

  get answerControl() {
    return this.form.get('answer');
  }

  matchAnswer(optValue: string, expectedAnswer: string): boolean {
    if (!optValue || !expectedAnswer) return false;
    if (optValue === expectedAnswer) return true;
    
    // Handle the case where expectedAnswer is "A" and optValue is "A. text"
    const expectedTrimmed = expectedAnswer.trim().toUpperCase();
    if (/^[A-D]$/.test(expectedTrimmed)) {
      const match = optValue.trim().match(/^([A-D])[.\s:]/i);
      if (match && match[1].toUpperCase() === expectedTrimmed) {
         return true;
      }
    }
    return false;
  }

  getParsedAnswers(): string[] {
    const val = this.answerControl?.value;
    if (!val) return [];
    
    if (typeof val === 'string') {
      try {
        return val.startsWith('[') ? JSON.parse(val) : [val];
      } catch {
        return [val];
      }
    } else if (Array.isArray(val)) {
      return (val as any[]).map((v: any) => String(v));
    }
    return [String(val)];
  }

  isAnswerSelected(optValue: string): boolean {
    const answers = this.getParsedAnswers();
    return answers.some(a => this.matchAnswer(optValue, a));
  }

  setSingleAnswer(optValue: string) {
    this.answerControl?.setValue(JSON.stringify([optValue]));
  }

  toggleMultiAnswer(optValue: string) {
    let answers = this.getParsedAnswers();

    // Check if the optValue is already matched by any answer in the array
    const matchedAnswerIndex = answers.findIndex(a => this.matchAnswer(optValue, a));
    
    if (matchedAnswerIndex > -1) {
      answers.splice(matchedAnswerIndex, 1);
    } else {
      answers.push(optValue);
    }
    this.answerControl?.setValue(JSON.stringify(answers));
  }
}
