import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { LoginComponent } from './features/auth/login/login.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'ca/dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['CA', 'Admin'] }
  },
  {
    path: 'approver/dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['Approver', 'Admin'] }
  }
];
