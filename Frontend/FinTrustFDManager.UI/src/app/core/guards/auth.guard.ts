import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

export const authGuard: CanActivateFn = (route) => {
  const router = inject(Router);

  // Retrieve token from localStorage
  const token = localStorage.getItem('token');
  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  // Retrieve required roles for the route
  const allowedRoles = route.data?.['roles'] as Array<string>;
  if (!allowedRoles || allowedRoles.length === 0) {
    // If no roles specified, just being logged in is enough
    return true;
  }

  // Get current user role
  const role = localStorage.getItem('role');
  if (role && allowedRoles.includes(role)) {
    return true;
  }

  // Fallback if not authorized
  router.navigate(['/login']);
  return false;
};
