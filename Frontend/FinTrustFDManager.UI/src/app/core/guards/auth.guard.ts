import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

/**
 * Resolves the default dashboard route for a given role.
 * Used when an authenticated user is redirected away from a route
 * they don't have access to — they should land on their own dashboard.
 */
function getDefaultRouteForRole(role: string): string {
  const normalized = role?.toLowerCase();
  if (normalized === 'admin') return '/admin/dashboard';
  if (normalized === 'approver') return '/approver/dashboard';
  if (normalized === 'ca') return '/ca/dashboard';
  return '/dashboard';
}

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

  // Get current user role (case-insensitive comparison)
  const role = localStorage.getItem('role');
  if (role && allowedRoles.some(r => r.toLowerCase() === role.toLowerCase())) {
    return true;
  }

  // If authenticated but not authorized for this route,
  // redirect to their own dashboard instead of login page.
  if (role) {
    router.navigate([getDefaultRouteForRole(role)]);
  } else {
    router.navigate(['/login']);
  }
  return false;
};
