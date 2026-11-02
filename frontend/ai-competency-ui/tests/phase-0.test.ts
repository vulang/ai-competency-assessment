import '@angular/compiler';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createEnvironmentInjector, runInInjectionContext, Injector, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { HttpClient, HttpBackend, HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { AccountService } from '../src/app/core/auth/account.service';
import { AuthService } from '../src/app/core/auth/auth.service';
import { adminRoleGuard, candidateRoleGuard } from '../src/app/core/auth/role.guard';
import { CandidateRegisterComponent } from '../src/app/features/candidate/register/candidate-register.component';
import { of, throwError, firstValueFrom } from 'rxjs';
import { authInterceptor } from '../src/app/core/auth/auth.interceptor';
import { FrameworkService } from '../src/app/core/competency/framework.service';
import { QuestionService } from '../src/app/core/questions/question.service';
import { QuestionFormComponent } from '../src/app/features/admin/questions/question-form/question-form.component';
import { environment } from '../src/environments/environment';

class MemoryStorage {
  private readonly data = new Map<string, string>();
  getItem(key: string) { return this.data.get(key) ?? null; }
  setItem(key: string, value: string) { this.data.set(key, value); }
  removeItem(key: string) { this.data.delete(key); }
  clear() { this.data.clear(); }
  key(index: number) { return [...this.data.keys()][index] ?? null; }
  get length() { return this.data.size; }
}
const storage = new MemoryStorage();
Object.assign(globalThis, {
  sessionStorage: storage,
  localStorage: new MemoryStorage(),
  window: { location: { origin: 'http://localhost:4200' }, atob: globalThis.atob, sessionStorage: storage, localStorage: new MemoryStorage() }
});

function token(role: unknown, exp = Date.now() / 1000 + 3600) {
  return `header.${Buffer.from(JSON.stringify({ sub: 'candidate-1', role, exp })).toString('base64url')}.signature`;
}

function registrationFixture() {
  const injector = createEnvironmentInjector([
    provideHttpClientTesting(),
    { provide: HttpClient, useFactory: () => new HttpClient(inject(HttpBackend)) }, FormBuilder, AccountService
  ], Injector.NULL as any);
  const component = runInInjectionContext(injector, () => new CandidateRegisterComponent());
  return { injector, component, http: injector.get(HttpTestingController) };
}

function validForm(component: CandidateRegisterComponent) {
  component.form.setValue({ email: 'candidate@example.com', password: 'Strong123!', confirmPassword: 'Strong123!' });
}

test('registration validates email, password strength, and confirmation before calling API', () => {
  const { injector, component, http } = registrationFixture();
  try {
    component.submit();
    assert.equal(component.form.invalid, true);
    http.expectNone(`${environment.apiUrl}/api/account/register`);
    validForm(component);
    assert.equal(component.form.valid, true);
    component.form.controls.confirmPassword.setValue('Different123!');
    component.submit();
    assert.equal(component.form.hasError('passwordMismatch'), true);
    http.expectNone(`${environment.apiUrl}/api/account/register`);
    component.form.patchValue({ email: 'invalid', password: 'alllowercase', confirmPassword: 'alllowercase' });
    assert.equal(component.form.controls.email.invalid, true);
    assert.equal(component.form.controls.password.invalid, true);
  } finally { http.verify(); injector.destroy(); }
});

test('registration sends only email and password, blocks repeat submit, and clears passwords on success', () => {
  const { injector, component, http } = registrationFixture();
  try {
    validForm(component);
    component.submit();
    component.submit();
    const request = http.expectOne(`${environment.apiUrl}/api/account/register`);
    assert.equal(request.request.method, 'POST');
    assert.deepEqual(request.request.body, { email: 'candidate@example.com', password: 'Strong123!' });
    request.flush({ email: 'candidate@example.com', message: 'Account created' }, { status: 201, statusText: 'Created' });
    assert.equal(component.created, true);
    assert.equal(component.isLoading, false);
    assert.equal(component.form.controls.password.value, '');
    assert.equal(component.form.controls.confirmPassword.value, '');
    component.submit();
    http.expectNone(`${environment.apiUrl}/api/account/register`);
  } finally { http.verify(); injector.destroy(); }
});

test('registration displays server validation errors and can retry after failure', () => {
  const { injector, component, http } = registrationFixture();
  try {
    validForm(component);
    component.submit();
    http.expectOne(`${environment.apiUrl}/api/account/register`).flush(
      { errors: { email: ['Email is already registered.'] } }, { status: 400, statusText: 'Bad Request' });
    assert.deepEqual(component.errors, ['Email is already registered.']);
    assert.equal(component.isLoading, false);
    component.submit();
    http.expectOne(`${environment.apiUrl}/api/account/register`).flush(
      { detail: 'Registration unavailable.' }, { status: 503, statusText: 'Unavailable' });
    assert.deepEqual(component.errors, ['Registration unavailable.']);
  } finally { http.verify(); injector.destroy(); }
});

test('auth preserves all role claims and expired identities do not grant access', () => {
  storage.clear();
  const auth = new AuthService();
  assert.equal(auth.loginWithToken('encrypted-access-token', token(['Candidate', 'Admin'])), true);
  assert.equal(auth.hasRole('Admin'), true);
  assert.equal(auth.homePath(), '/admin');
  assert.equal(auth.loginWithToken('encrypted-access-token', token('Candidate')), true);
  assert.equal(auth.hasRole('Admin'), false);
  assert.equal(auth.homePath(), '/candidate/home');
  assert.equal(auth.loginWithToken('encrypted-access-token', token('Admin', 1)), false);
  assert.equal(auth.hasRole('Admin'), false);
  assert.equal(auth.currentUser(), null);
});

test('admin and candidate guards redirect anonymous users and deny accounts without an allowed role', async () => {
  let authenticated = false;
  let roles: string[] = [];
  const injector = createEnvironmentInjector([
    { provide: AuthService, useValue: { checkAuthAsync: async () => authenticated, hasRole: (role: string) => roles.includes(role) } },
    { provide: Router, useValue: { createUrlTree: (paths: string[]) => ({ paths }) } }
  ], Injector.NULL as any);
  const invoke = (guard: typeof adminRoleGuard) => runInInjectionContext(injector, () => guard({} as any, {} as any));
  try {
    assert.deepEqual(await invoke(adminRoleGuard), { paths: ['/login'] });
    assert.deepEqual(await invoke(candidateRoleGuard), { paths: ['/candidate/login'] });
    authenticated = true;
    roles = ['Candidate'];
    assert.deepEqual(await invoke(adminRoleGuard), { paths: ['/access-denied'] });
    assert.equal(await invoke(candidateRoleGuard), true);
    roles = ['Admin'];
    assert.equal(await invoke(adminRoleGuard), true);
    assert.equal(await invoke(candidateRoleGuard), true);
    roles = ['Annotator'];
    assert.deepEqual(await invoke(candidateRoleGuard), { paths: ['/access-denied'] });
  } finally { injector.destroy(); }
});


test('auth interceptor sends bearer token only to protected API on the configured origin', () => {
  const injector = createEnvironmentInjector([
    { provide: AuthService, useValue: { isLoggedIn: () => true, currentUser: () => ({ accessToken: 'secret' }) } },
    { provide: Router, useValue: {} }
  ], Injector.NULL as any);
  try {
    for (const [url, expected] of [
      [`${environment.apiUrl}/api/questions`, 'Bearer secret'],
      [`${environment.apiUrl}/api/account/register`, null],
      [`${environment.apiUrl}/connect/token`, null],
      ['https://example.com/api/questions', null],
      ['http://localhost.example.com:5000/api/questions', null]
    ]) {
      runInInjectionContext(injector, () => authInterceptor(new HttpRequest('GET', url!), request => {
        assert.equal(request.headers.get('Authorization'), expected);
        return of(new HttpResponse({ status: 200 }));
      })).subscribe();
    }
  } finally { injector.destroy(); }
});

test('protected API 401 clears login; rejected login requests and 403 do not log the user out', async () => {
  let invalidated = 0;
  const destinations: string[][] = [];
  const injector = createEnvironmentInjector([
    { provide: AuthService, useValue: {
      isLoggedIn: () => true, currentUser: () => ({ accessToken: 'secret' }),
      invalidateSession: async () => { invalidated++; }
    } },
    { provide: Router, useValue: { navigate: (paths: string[]) => destinations.push(paths) } }
  ], Injector.NULL as any);
  try {
    for (const [path, status] of [['/connect/token', 401], ['/api/questions', 403], ['/api/questions', 401]] as const) {
      const result = runInInjectionContext(injector, () => authInterceptor(
        new HttpRequest('GET', `${environment.apiUrl}${path}`),
        () => throwError(() => new HttpErrorResponse({ status }))));
      await assert.rejects(firstValueFrom(result));
    }
    assert.equal(invalidated, 1);
    assert.deepEqual(destinations, [['/candidate/login']]);
  } finally { injector.destroy(); }
});


test('question editor uses catalogue skills and keeps difficulty independent of competency level', () => {
  let saved: any;
  const injector = createEnvironmentInjector([
    FormBuilder,
    { provide: FrameworkService, useValue: { getActive: () => of({ domains: [
      { name: 'Data', skills: [{ id: 2, isDefault: true }] },
      { name: 'AI Tools', skills: [{ id: 6, isDefault: true }] }
    ] }) } },
    { provide: QuestionService, useValue: { createQuestion: (value: unknown) => { saved = value; return of(value); } } },
    { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({})) } },
    { provide: Router, useValue: { navigate: () => {} } }
  ], Injector.NULL as any);
  try {
    const editor = runInInjectionContext(injector, () => new QuestionFormComponent());
    editor.ngOnInit();
    editor.form.patchValue({ prompt: 'Q', competencyGroupId: 'Data', competencyLevelId: 'Advanced' });
    editor.changeGroup();
    assert.equal(editor.form.controls.skillId.value, 2);
    assert.equal(editor.form.controls.difficulty.value, null);
    editor.form.patchValue({ competencyGroupId: 'AI Tools', difficulty: 1 });
    editor.changeGroup();
    assert.equal(editor.form.controls.skillId.value, 6);
    assert.equal(editor.form.valid, true);
    editor.onSubmit();
    assert.equal(saved.skillId, 6);
    assert.equal(saved.difficulty, 1);
    assert.equal(saved.competencyLevelId, 'Advanced');
    assert.equal(editor.isSaving(), false);
  } finally { injector.destroy(); }
});

test('question editor preserves skill, difficulty, and tags when editing and surfaces save failures', () => {
  const injector = createEnvironmentInjector([
    FormBuilder,
    { provide: FrameworkService, useValue: { getActive: () => of({ domains: [
      { name: 'Data', skills: [{ id: 2, isDefault: true }] }
    ] }) } },
    { provide: QuestionService, useValue: {
      getQuestion: () => of({ id: '42', prompt: 'Q', title: 'Title', type: 'mcq_single',
        competencyGroupId: 'Data', competencyLevelId: 'Basic', skillId: 2, difficulty: 4,
        tags: ['keep-me'], status: 'draft', options: ['A', 'B'], answer: ['A'] }),
      updateQuestion: () => throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { classification: ['Choose a supported group.'] } } }))
    } },
    { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: '42' })) } },
    { provide: Router, useValue: { navigate: () => {} } }
  ], Injector.NULL as any);
  try {
    const editor = runInInjectionContext(injector, () => new QuestionFormComponent());
    editor.ngOnInit();
    assert.equal(editor.form.controls.skillId.value, 2);
    assert.equal(editor.form.controls.difficulty.value, 4);
    assert.deepEqual(editor.form.controls.tags.value, ['keep-me']);
    assert.equal(editor.form.controls.answer.value, '["A"]');
    editor.onSubmit();
    assert.equal(editor.isSaving(), false);
    assert.equal(editor.errorMessage(), 'Choose a supported group.');
  } finally { injector.destroy(); }
});
