import '@angular/compiler';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createEnvironmentInjector, runInInjectionContext, Injector, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { TestService } from '../src/app/core/tests/test.service';
import { ReportService } from '../src/app/core/reports/report.service';
import { QuestionService } from '../src/app/core/questions/question.service';
import { LevelMappingService } from '../src/app/core/tests/level-mapping.service';
import {
  blueprintSummary, defaultQuotas, levelMixError, standardIssues, totalQuota
} from '../src/app/core/tests/blueprint';
import { TestBuilderComponent } from '../src/app/features/tests/test-builder/test-builder.component';
import { ItemAnalysisComponent } from '../src/app/features/tests/item-analysis/item-analysis.component';
import { LevelMappingComponent } from '../src/app/features/tests/level-mapping/level-mapping.component';
import { ItemAnalysisReport, SaveTestRequest, TestPreview } from '../src/app/models/test.model';
import { environment } from '../src/environments/environment';

function httpFixture<T>(service: new () => T) {
  const injector = createEnvironmentInjector([
    provideHttpClientTesting(),
    { provide: HttpClient, useFactory: () => new HttpClient(inject(HttpBackend)) }, service
  ], Injector.NULL as any);
  return { injector, service: injector.get(service), http: injector.get(HttpTestingController) };
}

test('standard form rules match the thesis short form of 5–6 items per group and 35–42 in total', () => {
  assert.equal(totalQuota(defaultQuotas(6)), 42);
  assert.deepEqual(standardIssues(defaultQuotas(5)), []);
  assert.deepEqual(standardIssues(defaultQuotas(6)), []);
  assert.equal(standardIssues({ ...defaultQuotas(6), DATA: 4 }).length, 1);
  assert.equal(standardIssues({ ...defaultQuotas(5), DATA: 4 }).length, 2);
  assert.equal(standardIssues({ ...defaultQuotas(6), DATA: 7 }).length, 2);
  assert.equal(levelMixError({}), null);
  assert.equal(levelMixError({ 1: 30, 2: 40, 3: 30 }), null);
  assert.match(levelMixError({ 1: 50, 2: 40, 3: 30 })!, /120/);
  assert.equal(blueprintSummary({ groupQuotas: defaultQuotas(5), levelMix: { 1: 40, 2: 60 }, seed: 9, passPercent: 70 }),
    '35 items · levels B 40% / I 60% · seed 9');
});

test('test service sends blueprint, ordering and lifecycle requests to the tests API', () => {
  const { injector, service, http } = httpFixture(TestService);
  try {
    const request: SaveTestRequest = { name: 'Form', description: '', durationMinutes: 45, passPercent: 70, isStandard: true,
      groupQuotas: defaultQuotas(5), levelMix: {}, seed: null };
    service.preview(request).subscribe();
    const preview = http.expectOne(`${environment.apiUrl}/api/tests/preview`);
    assert.equal(preview.request.method, 'POST');
    assert.deepEqual(preview.request.body, request);
    preview.flush({});
    service.setQuestions('7', [3, 1, 2]).subscribe();
    const order = http.expectOne(`${environment.apiUrl}/api/tests/7/questions`);
    assert.equal(order.request.method, 'PUT');
    assert.deepEqual(order.request.body, { questionIds: [3, 1, 2] });
    order.flush({});
    service.assemble('7', null).subscribe();
    assert.deepEqual(http.expectOne(`${environment.apiUrl}/api/tests/7/assemble`).request.body, { seed: null });
    service.publish('7').subscribe();
    assert.equal(http.expectOne(`${environment.apiUrl}/api/tests/7/publish`).request.method, 'POST');
    service.exportItemAnalysis('7', 'responses').subscribe();
    const csv = http.expectOne(r => r.url === `${environment.apiUrl}/api/tests/7/item-analysis/export`);
    assert.equal(csv.request.params.get('kind'), 'responses');
    assert.equal(csv.request.responseType, 'blob');
  } finally { injector.destroy(); }
});

test('report service filters by test only when one is chosen', () => {
  const { injector, service, http } = httpFixture(ReportService);
  try {
    service.getSummary(null).subscribe();
    assert.equal(http.expectOne(`${environment.apiUrl}/api/reports/summary`).request.params.keys().length, 0);
    service.getAttempts(4).subscribe();
    assert.equal(http.expectOne(r => r.url === `${environment.apiUrl}/api/reports/attempts`).request.params.get('examId'), '4');
    http.verify();
  } finally { injector.destroy(); }
});

function builderFixture() {
  const created: SaveTestRequest[] = [];
  const navigations: unknown[] = [];
  const preview: TestPreview = { seed: 1234, questions: [], coverage: [], warnings: [], standardIssues: [] };
  const injector = createEnvironmentInjector([
    FormBuilder,
    { provide: TestService, useValue: {
      preview: () => of(preview),
      createTest: (request: SaveTestRequest) => { created.push(request); return of({ id: '15' }); }
    } },
    { provide: QuestionService, useValue: { getQuestions: () => of([]) } },
    { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({})) } },
    { provide: Router, useValue: { navigate: (paths: unknown) => navigations.push(paths) } }
  ], Injector.NULL as any);
  const component = runInInjectionContext(injector, () => new TestBuilderComponent());
  return { injector, component, created, navigations };
}

test('test builder validates quotas and level mix before saving and keeps the previewed seed', () => {
  const { injector, component, created, navigations } = builderFixture();
  try {
    component.form.controls.name.setValue('Standard form A');
    assert.equal(component.form.valid, true);
    assert.equal(component.quotaTotal, 42);
    assert.deepEqual(component.quotaIssues, []);

    component.form.controls.groupQuotas.controls['DATA'].setValue(4);
    assert.equal(component.quotaIssues.length, 1);
    component.form.controls.isStandard.setValue(false);
    assert.deepEqual(component.quotaIssues, []);

    component.form.controls.levelMix.setValue({ 1: 50, 2: 40, 3: 30 });
    assert.match(component.levelMixMessage!, /must add up to 100/);
    component.save();
    assert.equal(created.length, 0);

    component.form.controls.levelMix.setValue({ 1: 0, 2: 0, 3: 0 });
    component.form.controls.groupQuotas.setValue(defaultQuotas(0));
    assert.equal(component.form.controls.groupQuotas.hasError('emptyBlueprint'), true);

    component.form.controls.groupQuotas.setValue(defaultQuotas(5));
    component.form.controls.isStandard.setValue(true);
    component.runPreview();
    assert.equal(component.form.controls.seed.value, 1234);
    component.save();
    assert.equal(created.length, 1);
    assert.equal(created[0].seed, 1234);
    assert.equal(totalQuota(created[0].groupQuotas), 35);
    assert.deepEqual(navigations, [['/admin/tests', '15']]);
  } finally { injector.destroy(); }
});

test('item analysis joins question details to CTT rows and filters flagged items', () => {
  const report: ItemAnalysisReport = {
    examId: 3, examTitle: 'Form', examStatus: 'active', generatedAt: '', excludedNonObjective: 0,
    items: [
      { questionId: 10, orderIndex: 1, title: 'Good item', type: 'mcq_single', groupCode: 'DATA', targetLevel: 1, options: ['A', 'B'] },
      { questionId: 11, orderIndex: 2, title: 'Hard item', type: 'mcq_single', groupCode: 'DATA', targetLevel: 3, options: ['A', 'B'] }
    ],
    analysis: {
      nRespondents: 12, nItems: 2,
      total: { mean: 1, sd: 0.5, min: 0, max: 2, histogram: [] },
      reliability: { nItems: 2, alpha: 0.4, omega: null, excludedItems: [], notes: [] },
      groups: [],
      items: [
        { itemId: '10', group: 'DATA', n: 12, p: 0.6, itemRestR: 0.4, itemRestRGroup: 0.4, discriminationIndex: 0.5, alphaIfDeleted: null, omitRate: 0, flags: [], distractors: [] },
        { itemId: '11', group: 'DATA', n: 12, p: 0.1, itemRestR: 0.1, itemRestRGroup: 0.1, discriminationIndex: 0, alphaIfDeleted: null, omitRate: 0, flags: ['too_hard', 'low_discrimination'], distractors: [] }
      ],
      thresholds: { minDifficulty: 0.2, maxDifficulty: 0.9, minDiscrimination: 0.2, minDistractorRate: 0.05 }
    }
  };
  const injector = createEnvironmentInjector([
    { provide: TestService, useValue: { itemAnalysis: (id: string) => { assert.equal(id, '3'); return of(report); } } },
    { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '3' }) } } }
  ], Injector.NULL as any);
  try {
    const component = runInInjectionContext(injector, () => new ItemAnalysisComponent());
    assert.equal(component.rows().length, 2);
    assert.equal(component.rows()[1].meta.title, 'Hard item');
    assert.equal(component.flaggedCount(), 1);
    component.flaggedOnly.set(true);
    assert.deepEqual(component.rows().map(r => r.stats.itemId), ['11']);
    assert.equal(component.optionLabel(2), 'C');
  } finally { injector.destroy(); }
});

test('level mapping editor requires ordered thresholds and a named approver', () => {
  const published: unknown[] = [];
  const draft = { id: 1, version: 'tl-v1-draft', status: 'Draft', isActive: false, intermediateMinPercent: 50,
    advancedMinPercent: 80, minItemsPerGroup: 5, source: '', reviewedBy: null, reviewNotes: null, publishedAt: null, createdAt: '' };
  const injector = createEnvironmentInjector([
    FormBuilder,
    { provide: LevelMappingService, useValue: {
      list: () => of([draft]),
      publish: (...args: unknown[]) => { published.push(args); return of({ mapping: { ...draft, status: 'Published' }, backfilledGroupScores: 3 }); }
    } }
  ], Injector.NULL as any);
  try {
    const component = runInInjectionContext(injector, () => new LevelMappingComponent());
    assert.equal(component.selected()?.version, 'tl-v1-draft');
    component.form.controls.advancedMinPercent.setValue(40);
    assert.equal(component.form.hasError('thresholds'), true);
    component.form.controls.advancedMinPercent.setValue(80);
    assert.equal(component.form.valid, true);
    component.publish();
    assert.equal(published.length, 0);
    globalThis.confirm = () => true;
    component.reviewForm.setValue({ reviewedBy: 'Expert panel', reviewNotes: 'Approved' });
    component.publish();
    assert.deepEqual(published, [['tl-v1-draft', 'Expert panel', 'Approved']]);
    assert.match(component.message()!, /3 earlier group score/);
  } finally { injector.destroy(); }
});
