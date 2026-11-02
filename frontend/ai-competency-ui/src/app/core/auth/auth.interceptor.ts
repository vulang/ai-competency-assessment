import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const api = new URL(environment.apiUrl || window.location.origin);
  const target = new URL(req.url, window.location.origin);
  const protectedApi = target.origin === api.origin && target.pathname.startsWith('/api/') &&
    target.pathname !== '/api/account/register';
  const token = protectedApi && auth.isLoggedIn() ? auth.currentUser()?.accessToken : undefined;
  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(catchError((error: HttpErrorResponse) => {
    if (protectedApi && error.status === 401) {
      void auth.invalidateSession().then(() => router.navigate(['/candidate/login']));
    }
    return throwError(() => error);
  }));
};
