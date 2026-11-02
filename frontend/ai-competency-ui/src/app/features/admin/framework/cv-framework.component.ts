import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CvFramework, CvFrameworkService, CvGroup, CvIndicator, CvRubric, CvVersion } from '../../../core/competency/cv-framework.service';

@Component({
  selector: 'app-cv-framework', standalone: true, imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './cv-framework.component.html', styleUrl: './cv-framework.component.scss'
})
export class CvFrameworkComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CvFrameworkService);
  versions = signal<CvVersion[]>([]);
  framework = signal<CvFramework | null>(null);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  busy = signal(false);
  cloneForm = this.fb.nonNullable.group({ version: ['', [Validators.required, Validators.pattern(/^[a-z0-9][a-z0-9._-]{2,63}$/)]] });
  reviewForm = this.fb.nonNullable.group({ reviewedBy: ['', Validators.required], reviewNotes: ['', Validators.required] });
  form = this.fb.nonNullable.group({ groups: this.fb.array<ReturnType<CvFrameworkComponent['groupForm']>>([]) });

  ngOnInit() { this.refresh(); }
  get groups() { return this.form.controls.groups; }
  indicators(groupIndex: number) { return this.groups.at(groupIndex).controls.indicators; }
  rubricRules(groupIndex: number) { return this.groups.at(groupIndex).controls.rubricRules; }

  private groupForm(group: CvGroup) {
    return this.fb.nonNullable.group({
      code: [group.code], nameVi: [group.nameVi, Validators.required], nameEn: [group.nameEn, Validators.required],
      order: [group.order],
      indicators: this.fb.array(group.indicators.map(i => this.indicatorForm(i))),
      rubricRules: this.fb.array(group.rubricRules.map(r => this.rubricForm(r)))
    });
  }
  private indicatorForm(item: CvIndicator) {
    return this.fb.nonNullable.group({ code: [item.code, Validators.required], level: [item.level],
      textVi: [item.textVi, Validators.required], textEn: [item.textEn, Validators.required] });
  }
  private rubricForm(item: CvRubric) {
    return this.fb.nonNullable.group({ level: [item.level], acceptedEvidence: [item.acceptedEvidence, Validators.required],
      insufficientEvidence: [item.insufficientEvidence, Validators.required], example: [item.example, Validators.required] });
  }
  addIndicator(groupIndex: number, level: number) {
    const group = this.groups.at(groupIndex).value;
    const existing = this.indicators(groupIndex).controls.filter(i => i.controls.level.value === level).length;
    if (existing >= 6) return;
    const next = Math.max(0, ...this.indicators(groupIndex).controls
      .filter(i => i.controls.level.value === level)
      .map(i => Number(i.controls.code.value.split('.').at(-1)) || 0)) + 1;
    this.indicators(groupIndex).push(this.indicatorForm({ code: `${group.code}.${level}.${next}`,
      level, textVi: '', textEn: '' }));
  }
  removeIndicator(groupIndex: number, index: number) {
    const level = this.indicators(groupIndex).at(index).controls.level.value;
    if (this.indicators(groupIndex).controls.filter(i => i.controls.level.value === level).length <= 3) return;
    this.indicators(groupIndex).removeAt(index);
  }
  refresh(select?: string) {
    this.api.versions().subscribe({ next: versions => {
      this.versions.set(versions);
      const version = select ?? this.framework()?.version ?? versions.at(-1)?.version;
      if (version) this.select(version);
    }, error: e => this.fail(e) });
  }
  select(version: string) {
    this.api.detail(version).subscribe({ next: detail => {
      this.framework.set(detail);
      this.groups.clear();
      detail.groups.forEach(group => this.groups.push(this.groupForm(group)));
      this.form.markAsPristine();
      this.error.set(null); this.success.set(null);
    }, error: e => this.fail(e) });
  }
  clone() {
    const source = this.framework();
    if (!source || this.cloneForm.invalid || this.busy()) return;
    this.busy.set(true);
    this.api.clone(source.version, this.cloneForm.controls.version.value).subscribe({
      next: result => { this.busy.set(false); this.cloneForm.reset(); this.refresh(result.version); },
      error: e => this.fail(e)
    });
  }
  save() {
    const version = this.framework();
    if (!version || version.status !== 'Draft' || this.form.invalid || this.busy()) return;
    this.busy.set(true);
    this.api.save(version.version, this.groups.getRawValue()).subscribe({
      next: () => { this.busy.set(false); this.select(version.version); this.success.set('Draft saved.'); },
      error: e => this.fail(e)
    });
  }
  publish() {
    const version = this.framework();
    if (!version || version.status !== 'Draft' || this.reviewForm.invalid || this.form.dirty || this.busy()) return;
    this.busy.set(true);
    const review = this.reviewForm.getRawValue();
    this.api.publish(version.version, review.reviewedBy, review.reviewNotes).subscribe({
      next: () => { this.busy.set(false); this.reviewForm.reset(); this.refresh(version.version); },
      error: e => this.fail(e)
    });
  }
  private fail(error: HttpErrorResponse) {
    this.busy.set(false);
    this.error.set(typeof error.error === 'string' ? error.error : error.error?.title ?? 'Request failed. Reload and try again.');
  }
}
