import { Routes } from '@angular/router';

import { DashboardComponent } from './features/dashboard/dashboard.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { LoginComponent } from './features/auth/login/login.component';

import { authGuard } from './core/guards/auth.guard';

import { EntityListComponent } from './features/entity/entity-list/entity-list';
import { CashFlowComponent } from './features/cash-flow/cash-flow';
import { FDListComponent } from './features/fd/fd-list/fd-list.component';
import { FDDetailComponent } from './features/fd/fd-detail/fd-detail.component';

import { CounterpartiesComponent } from './features/counterparties/counterparties.component';
import { ApproverDashboardComponent } from './features/approver/approver-dashboard/approver-dashboard.component';
import { ApproverPendingComponent } from './features/approver/approver-pending/approver-pending.component';
import { ApproverListComponent } from './features/approver/approver-list/approver-list.component';
import { ApproverDetailComponent } from './features/approver/approver-detail/approver-detail.component';

export const routes: Routes = [

  // =========================================
  // DEFAULT
  // =========================================

  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },

  // =========================================
  // AUTH
  // =========================================

  {
    path: 'login',
    component: LoginComponent
  },

  {
    path: 'register',
    component: RegisterComponent
  },

  // =========================================
  // DASHBOARD
  // =========================================

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },

  {
    path: 'admin/dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: {
      roles: ['Admin']
    }
  },

  {
    path: 'ca/dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: {
      roles: ['CA', 'Admin']
    }
  },

  {
    path: 'approver/dashboard',
    component: ApproverDashboardComponent,
    canActivate: [authGuard],
    data: {
      roles: ['Approver']
    }
  },
  {
    path: 'approver/pending',
    component: ApproverPendingComponent,
    canActivate: [authGuard],
    data: {
      roles: ['Approver']
    }
  },
  {
    path: 'approver/list',
    component: ApproverListComponent,
    canActivate: [authGuard],
    data: {
      roles: ['Approver']
    }
  },
  {
    path: 'approver/detail/:id',
    component: ApproverDetailComponent,
    canActivate: [authGuard],
    data: {
      roles: ['Approver']
    }
  },

  // =========================================
  // ENTITIES
  // =========================================

  {
    path: 'entities',
    component: EntityListComponent,
    canActivate: [authGuard]
  },

  {
    path: 'entities/:id/cash-flow',
    component: CashFlowComponent,
    canActivate: [authGuard]
  },

  // =========================================
  // COUNTRIES
  // =========================================

  {
    path: 'countries',
    loadComponent: () =>
      import('./features/country/countries.component')
        .then(m => m.CountriesComponent),

    canActivate: [authGuard]
  },

  // =========================================
  // CURRENCIES
  // =========================================

  {
    path: 'currencies',
    loadComponent: () =>
      import('./features/currencies/currencies.component')
        .then(m => m.CurrenciesComponent),

    canActivate: [authGuard]
  },

  // =========================================
  // COUNTERPARTIES
  // =========================================

  {
    path: 'counterparties',
    component: CounterpartiesComponent,
    canActivate: [authGuard]
  },

  // =========================================
  // BENCHMARKS
  // =========================================

  {
    path: 'benchmarks',
    loadComponent: () =>
      import('./features/benchmark/benchmark.component')
        .then(m => m.BenchmarkComponent),

    canActivate: [authGuard]
  },

  // ==============================
  // FD LANDING PAGE
  // ==============================
  {
    path: 'fd',
    component: FDListComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  // ==============================
  // ADD FD
  // ==============================
  {
    path: 'fd-detail',
    component: FDDetailComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  // ==============================
  // EDIT FD
  // ==============================
  {
    path: 'fd-detail/:id',
    component: FDDetailComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  }

];
// Trigger recompile
