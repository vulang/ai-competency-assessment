import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DomainScore {
  domainName: string;
  score: number;   // 0–100
  masteryCount: number;
  totalSkills: number;
}

export interface CompetencyProfile {
  profileId: string;
  userId: number;
  sessionId: string | null;
  theta: number;
  overallLevel: 'Foundation' | 'Apply' | 'Create';
  overallScore: number;
  domainScores: Record<string, number>;   // { "ai_fundamentals": 72.5, ... }
  generatedAt: string;
}

export interface IrtSessionEstimate {
  sessionId: string;
  theta: number;
  seTheta: number;
  method: string;
  computedAt: string;
  overallLevel: string;
}

export interface SkillMastery {
  skillId: number;
  masteryProb: number;
  mastered: boolean;
  responseCount: number;
  lastUpdated: string;
}

export interface IrtAdminStats {
  totalItems: number;
  calibratedItems: number;
  avgDifficulty: number;
  avgDiscrimination: number;
  items: IrtItemStat[];
}

export interface IrtItemStat {
  questionId: number;
  aParam: number;
  bParam: number;
  cParam: number;
  seB: number | null;
  isCalibrated: boolean;
  responseCount: number;
  calibratedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class CompetencyService {
  private readonly base = `${environment.apiUrl}/competency`;

  constructor(private http: HttpClient) {}

  // ── Candidate ─────────────────────────────────────────────────────────────

  getMyProfile(): Observable<CompetencyProfile> {
    return this.http.get<CompetencyProfile>(`${this.base}/profile`);
  }

  getMyProfileHistory(): Observable<CompetencyProfile[]> {
    return this.http.get<CompetencyProfile[]>(`${this.base}/profile/history`);
  }

  getSessionIrt(sessionId: string): Observable<IrtSessionEstimate> {
    return this.http.get<IrtSessionEstimate>(`${this.base}/sessions/${sessionId}/irt`);
  }

  getMySkillMastery(): Observable<SkillMastery[]> {
    return this.http.get<SkillMastery[]>(`${this.base}/skills`);
  }

  // ── Admin ─────────────────────────────────────────────────────────────────

  getCandidateProfile(userId: number): Observable<CompetencyProfile> {
    return this.http.get<CompetencyProfile>(`${this.base}/admin/candidates/${userId}/profile`);
  }

  getIrtStats(): Observable<IrtAdminStats> {
    return this.http.get<IrtAdminStats>(`${this.base}/admin/irt-stats`);
  }
}
