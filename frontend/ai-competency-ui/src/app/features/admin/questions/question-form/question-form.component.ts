import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { QuestionService } from '../../../../core/questions/question.service';
import { Question, QuestionType } from '../../../../models/question.model';
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
    title: [''],
    prompt: ['', Validators.required],
    type: ['single_choice', Validators.required],
    competencyGroupId: ['', Validators.required],
    competencyLevelId: ['', Validators.required],
    status: ['draft', Validators.required],
    options: this.fb.array<string>([]),
    answer: [''],
    context: [''],
    responseStructure: this.fb.array<string>([]),
    rubric: this.fb.array<string>([]),
    judgePromptHints: [''],
    behaviorsAddressed: this.fb.array<string>([])
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
        this.form.patchValue({
          title: question.title,
          prompt: question.prompt,
          type: question.type,
          competencyGroupId: question.competencyGroupId,
          competencyLevelId: question.competencyLevelId,
          status: question.status,
          answer: question.answer,
          context: question.context,
          judgePromptHints: question.judgePromptHints
        });
        
        ['options', 'responseStructure', 'rubric', 'behaviorsAddressed'].forEach(arrName => {
          const dataArray = question[arrName as keyof Question] as string[] | undefined;
          if (dataArray && Array.isArray(dataArray)) {
            const arr = this.form.get(arrName) as FormArray;
            arr.clear();
            dataArray.forEach(item => arr.push(this.fb.control(item)));
          }
        });
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

  getArrayControls(name: string) {
    return (this.form.get(name) as FormArray).controls;
  }

  addArrayItem(name: string, val: string = '') {
    (this.form.get(name) as FormArray).push(this.fb.control(val));
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
