import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface FrameworkSkill {
  id: number;
  name: string;
  description: string;
  weight: number;
  isDefault: boolean;
  criteria: { id: number; name: string; level: number; description: string }[];
}
export interface FrameworkDomain {
  id: number;
  code: string;
  name: string;
  weight: number;
  skills: FrameworkSkill[];
}
export interface ActiveFramework {
  id: number;
  name: string;
  version: string;
  domains: FrameworkDomain[];
}

@Injectable({ providedIn: 'root' })
export class FrameworkService {
  private readonly http = inject(HttpClient);
  getActive() { return this.http.get<ActiveFramework>(`${environment.apiUrl}/api/framework/active`); }
}
