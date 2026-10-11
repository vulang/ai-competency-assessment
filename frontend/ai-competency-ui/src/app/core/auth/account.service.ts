import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RegisterCandidateRequest {
  email: string;
  password: string;
}

export interface RegistrationResult {
  email: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);

  register(request: RegisterCandidateRequest): Observable<RegistrationResult> {
    return this.http.post<RegistrationResult>(`${environment.apiUrl}/api/account/register`, request);
  }
}
