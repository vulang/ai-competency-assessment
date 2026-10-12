import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AccountService } from '../../../core/auth/account.service';

export function matchingPasswords(control: AbstractControl): ValidationErrors | null {
  return control.get('password')?.value === control.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-candidate-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './candidate-register.component.html',
  styleUrl: './candidate-register.component.css'
})
export class CandidateRegisterComponent {
  private readonly accounts = inject(AccountService);
  private readonly builder = inject(FormBuilder);
  readonly form = this.builder.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).+$/)]],
    confirmPassword: ['', Validators.required]
  }, { validators: matchingPasswords });
  isLoading = false;
  created = false;
  errors: string[] = [];

  submit(): void {
    if (this.isLoading || this.created) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.errors = [];
    this.isLoading = true;
    const { email, password } = this.form.getRawValue();
    this.accounts.register({ email: email.trim(), password }).subscribe({
      next: () => {
        this.created = true;
        this.isLoading = false;
        this.form.reset();
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading = false;
        const validation = error.error?.errors as Record<string, string[]> | undefined;
        this.errors = validation ? Object.values(validation).flat() :
          [error.error?.detail || 'Could not create your account. Please try again.'];
      }
    });
  }
}
