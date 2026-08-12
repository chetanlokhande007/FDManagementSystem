import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { LoginComponent } from './features/auth/login/login.component';
import { authGuard } from './core/guards/auth.guard';
import { EntityListComponent } from './features/entity/entity-list/entity-list';
import { EntityFormComponent } from './features/entity/entity-form/entity-form';
import { CashFlowComponent } from './features/cash-flow/cash-flow';

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
  },
  {
    path: 'entities',
    component: EntityListComponent,
    canActivate: [authGuard]
  },
  {
    path: 'entities/add',
    component: EntityFormComponent,
    canActivate: [authGuard]
  },
  {
    path: 'entities/edit/:id',
    component: EntityFormComponent,
    canActivate: [authGuard]
  },
  {
    path: 'entities/:id/cash-flow',
    component: CashFlowComponent,
    canActivate: [authGuard]
  },
  {
    path: 'countries',
    loadComponent: () => import('./features/country/countries.component').then(m => m.CountriesComponent),
    canActivate: [authGuard]
  }
];
