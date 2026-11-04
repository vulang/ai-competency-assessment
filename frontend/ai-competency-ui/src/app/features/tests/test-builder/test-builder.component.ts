import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TestService } from '../../../core/tests/test.service';
import { QuestionService } from '../../../core/questions/question.service';
import {
  COMPETENCY_GROUPS, LEVEL_NAMES, STANDARD_FORM, defaultQuotas, groupName, levelMixError, standardIssues, totalQuota
} from '../../../core/tests/blueprint';
import { LevelNumber, SaveTestRequest, TestDetail, TestPreview, TestQuestion } from '../../../models/test.model';
import { Question } from '../../../models/question.model';
import { apiError } from '../../../shared/download';

function quotaTotalValidator(control: AbstractControl): ValidationErrors | null {
  return totalQuota(control.value) > 0 ? null : { emptyBlueprint: true };
}

function levelMixValidator(control: AbstractControl): ValidationErrors | null {
  const message = levelMixError(control.value);
  return message ? { levelMix: message } : null;
}

@Component({
  selector: 'app-test-builder',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './test-builder.component.html',
  styleUrl: './test-builder.component.scss'
})
export class TestBuilderComponent {
  private readonly fb = inject(FormBuilder);
  private readonly testService = inject(TestService);
  private readonly questionService = inject(QuestionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly groups = COMPETENCY_GROUPS;
  readonly levels: LevelNumber[] = [1, 2, 3];
  readonly levelNames = LEVEL_NAMES;
  readonly standard = STANDARD_FORM;
  readonly groupName = groupName;

  testId = signal<string | null>(null);
  detail = signal<TestDetail | null>(null);
  preview = signal<TestPreview | null>(null);
  bank = signal<Question[]>([]);
  busy = signal(false);
  error = signal<string | null>(null);
  message = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(255)]],
    description: ['', Validators.maxLength(1000)],
    durationMinutes: [60, [Validators.required, Validators.min(1), Validators.max(600)]],
    passPercent: [70, [Validators.required, Validators.min(1), Validators.max(100)]],
    isStandard: [true],
    seed: this.fb.control<number | null>(null, Validators.min(1)),
    groupQuotas: this.fb.nonNullable.group(
      Object.fromEntries(COMPETENCY_GROUPS.map(g => [g.code, this.fb.nonNullable.control(6, [Validators.required, Validators.min(0), Validators.max(STANDARD_FORM.maxPerGroupAny)])])),
      { validators: quotaTotalValidator }),
    levelMix: this.fb.nonNullable.group({ 1: [30, [Validators.min(0), Validators.max(100)]], 2: [40, [Validators.min(0), Validators.max(100)]], 3: [30, [Validators.min(0), Validators.max(100)]] },
      { validators: levelMixValidator })
  });

  editable = computed(() => !this.testId() || this.detail()?.status === 'draft');
  questions = computed<TestQuestion[]>(() => this.detail()?.questions ?? this.preview()?.questions ?? []);
  coverage = computed(() => this.detail()?.coverage ?? this.preview()?.coverage ?? []);
  available = computed(() => {
    const used = new Set(this.questions().map(q => q.questionId));
    return this.bank().filter(q => q.eligible && !used.has(Number(q.id)));
  });

  constructor() {
    // Cloning navigates between two :id routes, which reuses this component.
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(params => {
      const id = params.get('id');
      this.testId.set(id);
      this.detail.set(null);
      this.preview.set(null);
      if (id) {
        this.testService.getTest(id).subscribe({
          next: detail => this.show(detail, this.message() ?? undefined),
          error: () => this.error.set('Could not load this test.')
        });
      } else {
        this.form.controls.groupQuotas.setValue(defaultQuotas(6));
      }
    });
    this.questionService.getQuestions().subscribe({ next: questions => this.bank.set(questions) });
  }

  /** Conformance of the assembled questions (from the API). */
  get issues(): string[] {
    return this.detail()?.standardIssues ?? (this.form.controls.isStandard.value ? this.preview()?.standardIssues ?? [] : []);
  }

  get quotaTotal(): number {
    return totalQuota(this.form.controls.groupQuotas.getRawValue());
  }

  /** Thesis conformance of the quotas themselves, before assembly. */
  get quotaIssues(): string[] {
    return this.form.controls.isStandard.value ? standardIssues(this.form.controls.groupQuotas.getRawValue()) : [];
  }

  get levelMixMessage(): string | null {
    return this.form.controls.levelMix.errors?.['levelMix'] ?? null;
  }

  runPreview(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.run(this.testService.preview(this.request()), preview => {
      this.preview.set(preview);
      this.form.controls.seed.setValue(preview.seed);
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const id = this.testId();
    if (!id) {
      this.run(this.testService.createTest(this.request()), detail => this.router.navigate(['/admin/tests', detail.id]));
      return;
    }
    this.run(this.testService.updateTest(id, this.request()), detail => this.show(detail, 'Blueprint saved. Reassemble to apply quota changes.'));
  }

  reassemble(newSeed: boolean): void {
    const seed = newSeed ? Math.floor(Math.random() * 2_000_000_000) + 1 : null;
    this.run(this.testService.assemble(this.testId()!, seed), detail => this.show(detail, 'Questions reassembled from the blueprint.'));
  }

  move(index: number, offset: number): void {
    const ids = this.questions().map(q => q.questionId);
    const target = index + offset;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    this.setQuestions(ids);
  }

  remove(index: number): void {
    this.setQuestions(this.questions().map(q => q.questionId).filter((_, i) => i !== index));
  }

  add(questionId: string): void {
    if (!questionId) return;
    this.setQuestions([...this.questions().map(q => q.questionId), Number(questionId)]);
  }

  publish(): void {
    if (!confirm('Publishing freezes the questions so every candidate answers the same form. Continue?')) return;
    this.run(this.testService.publish(this.testId()!), detail => this.show(detail, 'Test published.'));
  }

  archive(): void {
    if (!confirm('Archived tests can no longer be started. Continue?')) return;
    this.run(this.testService.archive(this.testId()!), detail => this.show(detail, 'Test archived.'));
  }

  clone(): void {
    this.run(this.testService.clone(this.testId()!), detail => {
      this.message.set('Cloned as a new draft.');
      this.router.navigate(['/admin/tests', detail.id]);
    });
  }

  deleteDraft(): void {
    if (!confirm('Delete this draft?')) return;
    this.run(this.testService.deleteTest(this.testId()!), () => this.router.navigate(['/admin/tests']));
  }

  private setQuestions(ids: number[]): void {
    this.run(this.testService.setQuestions(this.testId()!, ids), detail => this.show(detail));
  }

  private request(): SaveTestRequest {
    const value = this.form.getRawValue();
    return {
      name: value.name,
      description: value.description,
      durationMinutes: value.durationMinutes,
      passPercent: value.passPercent,
      isStandard: value.isStandard,
      seed: value.seed,
      groupQuotas: value.groupQuotas,
      levelMix: value.levelMix
    };
  }

  private show(detail: TestDetail, message?: string): void {
    this.detail.set(detail);
    this.preview.set(null);
    this.message.set(message ?? null);
    const spec = detail.blueprint;
    this.form.reset({
      name: detail.name,
      description: detail.description,
      durationMinutes: detail.durationMinutes,
      passPercent: spec?.passPercent ?? 70,
      isStandard: detail.isStandard,
      seed: spec?.seed ?? null,
      groupQuotas: { ...defaultQuotas(0), ...(spec?.groupQuotas ?? {}) },
      levelMix: { 1: spec?.levelMix?.[1] ?? 0, 2: spec?.levelMix?.[2] ?? 0, 3: spec?.levelMix?.[3] ?? 0 }
    });
    if (detail.status === 'draft') this.form.enable();
    else this.form.disable();
  }

  private run<T>(request: Observable<T>, next: (value: T) => void): void {
    this.busy.set(true);
    this.error.set(null);
    request.subscribe({
      next: value => { this.busy.set(false); next(value); },
      error: (error: HttpErrorResponse) => { this.busy.set(false); this.error.set(apiError(error, 'The request failed.')); }
    });
  }
}
