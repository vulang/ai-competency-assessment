import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ItemAnalysisExport, ItemAnalysisReport, SaveTestRequest, TestBlueprint, TestDetail, TestPreview
} from '../../models/test.model';

@Injectable({
  providedIn: 'root'
})
export class TestService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/tests`;

  getTests(): Observable<TestBlueprint[]> {
    return this.http.get<TestBlueprint[]>(this.apiUrl);
  }

  getTest(id: string): Observable<TestDetail> {
    return this.http.get<TestDetail>(`${this.apiUrl}/${id}`);
  }

  /** Assembles a form from the current bank without saving it. */
  preview(request: SaveTestRequest): Observable<TestPreview> {
    return this.http.post<TestPreview>(`${this.apiUrl}/preview`, request);
  }

  createTest(request: SaveTestRequest): Observable<TestDetail> {
    return this.http.post<TestDetail>(this.apiUrl, request);
  }

  updateTest(id: string, request: SaveTestRequest): Observable<TestDetail> {
    return this.http.put<TestDetail>(`${this.apiUrl}/${id}`, request);
  }

  assemble(id: string, seed: number | null): Observable<TestDetail> {
    return this.http.post<TestDetail>(`${this.apiUrl}/${id}/assemble`, { seed });
  }

  setQuestions(id: string, questionIds: number[]): Observable<TestDetail> {
    return this.http.put<TestDetail>(`${this.apiUrl}/${id}/questions`, { questionIds });
  }

  publish(id: string): Observable<TestDetail> {
    return this.http.post<TestDetail>(`${this.apiUrl}/${id}/publish`, {});
  }

  archive(id: string): Observable<TestDetail> {
    return this.http.post<TestDetail>(`${this.apiUrl}/${id}/archive`, {});
  }

  clone(id: string): Observable<TestDetail> {
    return this.http.post<TestDetail>(`${this.apiUrl}/${id}/clone`, {});
  }

  deleteTest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  itemAnalysis(id: string): Observable<ItemAnalysisReport> {
    return this.http.get<ItemAnalysisReport>(`${this.apiUrl}/${id}/item-analysis`);
  }

  exportItemAnalysis(id: string, kind: ItemAnalysisExport): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/item-analysis/export`, { params: { kind }, responseType: 'blob' });
  }
}
