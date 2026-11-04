import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FeatureFlags {
  /** Experimental IRT/CAT/BKT, outside the thesis scope. */
  adaptiveTesting: boolean;
}

@Injectable({ providedIn: 'root' })
export class FeatureService {
  private readonly http = inject(HttpClient);

  // Experimental features stay hidden if the flags cannot be read.
  readonly flags$: Observable<FeatureFlags> = this.http.get<FeatureFlags>(`${environment.apiUrl}/api/features`).pipe(
    catchError(() => of({ adaptiveTesting: false })),
    shareReplay(1)
  );
}
