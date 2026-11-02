import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function roleGuard(roles: readonly string[], loginPath: string): CanActivateFn {
  return async () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!await auth.checkAuthAsync()) return router.createUrlTree([loginPath]);
    return roles.some(role => auth.hasRole(role)) || router.createUrlTree(['/access-denied']);
  };
}

export const adminRoleGuard = roleGuard(['Admin'], '/login');
export const candidateRoleGuard = roleGuard(['Candidate', 'Admin'], '/candidate/login');
