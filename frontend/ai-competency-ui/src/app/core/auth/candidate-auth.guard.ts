import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const candidateAuthGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return true;
  }

  const isLoggedInAsync = await authService.checkAuthAsync();
  if (isLoggedInAsync) {
    return true;
  }

  return router.createUrlTree(['/candidate/login']);
};
