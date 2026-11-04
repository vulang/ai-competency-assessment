import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LevelMappingService } from '../../../core/tests/level-mapping.service';
import { LevelMapping } from '../../../models/test.model';
import { apiError } from '../../../shared/download';

function orderedThresholds(control: AbstractControl): ValidationErrors | null {
  const { intermediateMinPercent: low, advancedMinPercent: high } = control.value;
  return low > 0 && low < high && high <= 100 ? null : { thresholds: true };
}

@Component({
  selector: 'app-level-mapping',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './level-mapping.component.html'
})
export class LevelMappingComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(LevelMappingService);

  mappings = signal<LevelMapping[]>([]);
  selected = signal<LevelMapping | null>(null);
  creating = signal(false);
  error = signal<string | null>(null);
  message = signal<string | null>(null);
  active = computed(() => this.mappings().find(m => m.isActive) ?? null);

  form = this.fb.nonNullable.group({
    version: ['', [Validators.required, Validators.pattern(/^[a-z0-9][a-z0-9._-]{2,63}$/)]],
    intermediateMinPercent: [50, [Validators.required, Validators.min(0.01), Validators.max(99.99)]],
    advancedMinPercent: [80, [Validators.required, Validators.min(0.01), Validators.max(100)]],
    minItemsPerGroup: [5, [Validators.required, Validators.min(1), Validators.max(20)]],
    source: ['']
  }, { validators: orderedThresholds });

  reviewForm = this.fb.nonNullable.group({
    reviewedBy: ['', [Validators.required, Validators.maxLength(256)]],
    reviewNotes: ['']
  });

  constructor() {
    this.load();
  }

  load(select?: string): void {
    this.service.list().subscribe({
      next: mappings => {
        this.mappings.set(mappings);
        const chosen = mappings.find(m => m.version === select) ?? mappings.find(m => m.status === 'Draft') ?? mappings[0] ?? null;
        if (chosen) this.select(chosen);
      },
      error: () => this.error.set('Could not load level mappings.')
    });
  }

  select(mapping: LevelMapping): void {
    this.creating.set(false);
    this.selected.set(mapping);
    this.form.reset({
      version: mapping.version,
      intermediateMinPercent: mapping.intermediateMinPercent,
      advancedMinPercent: mapping.advancedMinPercent,
      minItemsPerGroup: mapping.minItemsPerGroup,
      source: mapping.source
    });
    this.form.controls.version.disable();
    if (mapping.status === 'Draft') this.form.enable({ emitEvent: false }); else this.form.disable({ emitEvent: false });
    this.form.controls.version.disable();
  }

  startNew(): void {
    const base = this.active() ?? this.selected();
    this.creating.set(true);
    this.selected.set(null);
    this.form.enable();
    this.form.reset({
      version: '',
      intermediateMinPercent: base?.intermediateMinPercent ?? 50,
      advancedMinPercent: base?.advancedMinPercent ?? 80,
      minItemsPerGroup: base?.minItemsPerGroup ?? 5,
      source: ''
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const request = this.creating()
      ? this.service.create(value)
      : this.service.update(this.selected()!.version, value);
    request.subscribe({
      next: mapping => { this.message.set(`Saved ${mapping.version}.`); this.load(mapping.version); },
      error: (error: HttpErrorResponse) => this.error.set(apiError(error, 'Could not save the mapping.'))
    });
  }

  publish(): void {
    const mapping = this.selected();
    if (!mapping || this.reviewForm.invalid) { this.reviewForm.markAllAsTouched(); return; }
    if (!confirm(`Publish ${mapping.version}? It becomes immutable and is used for all new submissions.`)) return;
    const { reviewedBy, reviewNotes } = this.reviewForm.getRawValue();
    this.service.publish(mapping.version, reviewedBy, reviewNotes).subscribe({
      next: result => {
        this.message.set(`Published ${result.mapping.version}; ${result.backfilledGroupScores} earlier group score(s) received levels.`);
        this.reviewForm.reset();
        this.load(result.mapping.version);
      },
      error: (error: HttpErrorResponse) => this.error.set(apiError(error, 'Could not publish the mapping.'))
    });
  }
}
