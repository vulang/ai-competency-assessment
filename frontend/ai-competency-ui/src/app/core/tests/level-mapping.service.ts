import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LevelMapping } from '../../models/test.model';

export interface SaveLevelMapping {
  version: string;
  intermediateMinPercent: number;
  advancedMinPercent: number;
  minItemsPerGroup: number;
  source: string;
}

@Injectable({ providedIn: 'root' })
export class LevelMappingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/test-level-mappings`;

  list(): Observable<LevelMapping[]> {
    return this.http.get<LevelMapping[]>(this.apiUrl);
  }

  create(request: SaveLevelMapping): Observable<LevelMapping> {
    return this.http.post<LevelMapping>(this.apiUrl, request);
  }

  update(version: string, request: SaveLevelMapping): Observable<LevelMapping> {
    return this.http.put<LevelMapping>(`${this.apiUrl}/${version}`, request);
  }

  publish(version: string, reviewedBy: string, reviewNotes: string): Observable<{ mapping: LevelMapping; backfilledGroupScores: number }> {
    return this.http.post<{ mapping: LevelMapping; backfilledGroupScores: number }>(`${this.apiUrl}/${version}/publish`, { reviewedBy, reviewNotes });
  }
}
