import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface CvIndicator { code: string; level: number; textVi: string; textEn: string; }
export interface CvRubric { level: number; acceptedEvidence: string; insufficientEvidence: string; example: string; }
export interface CvGroup {
  code: string; nameVi: string; nameEn: string; order: number;
  indicators: CvIndicator[]; rubricRules: CvRubric[];
}
export interface CvVersion { version: string; status: 'Draft' | 'Published'; isActive: boolean; publishedAt: string | null; source: string; }
export interface CvFramework extends CvVersion {
  levels: { code: string; ordinal: number; nameVi: string; nameEn: string }[];
  groups: CvGroup[];
}

@Injectable({ providedIn: 'root' })
export class CvFrameworkService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/framework/cv`;
  versions() { return this.http.get<CvVersion[]>(`${this.base}/versions`); }
  detail(version: string) { return this.http.get<CvFramework>(`${environment.apiUrl}/api/framework/${encodeURIComponent(version)}/indicators`); }
  clone(source: string, version: string) { return this.http.post<CvVersion>(`${this.base}/${encodeURIComponent(source)}/drafts`, { version }); }
  save(version: string, groups: CvGroup[]) { return this.http.put<void>(`${this.base}/${encodeURIComponent(version)}/content`, { groups }); }
  publish(version: string, reviewedBy: string, reviewNotes: string) {
    return this.http.post<CvVersion>(`${this.base}/${encodeURIComponent(version)}/publish`, { reviewedBy, reviewNotes });
  }
}
