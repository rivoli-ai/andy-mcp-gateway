import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'adapters',
    loadComponent: () => import('./features/adapters/adapters-list/adapters-list.component').then(m => m.AdaptersListComponent),
    canActivate: [authGuard]
  },
  {
    path: 'adapters/new',
    loadComponent: () => import('./features/adapters/adapter-form/adapter-form.component').then(m => m.AdapterFormComponent),
    canActivate: [authGuard]
  },
  {
    path: 'adapters/:id',
    loadComponent: () => import('./features/adapters/adapter-details/adapter-details.component').then(m => m.AdapterDetailsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'adapters/:id/edit',
    loadComponent: () => import('./features/adapters/adapter-form/adapter-form.component').then(m => m.AdapterFormComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];