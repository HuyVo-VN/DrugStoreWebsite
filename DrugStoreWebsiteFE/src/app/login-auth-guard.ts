import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './Services/auth.service';
import { inject } from '@angular/core';

export const loginAuthGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getAccessToken();
  const role = authService.getUserRole();

  if (token) {
    if (role === "Admin") {
      router.navigate(['/admin-page']);
    }
    else {
      router.navigate(['/']);
    }
    return false;
  }
  return true;
};
