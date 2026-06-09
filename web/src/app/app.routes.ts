import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './layout/shell';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.LoginComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        canActivate: [authGuard],
        data: { permission: 'dashboard.charts.read' },
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
      },
      {
        path: 'clients',
        canActivate: [authGuard],
        data: { permission: 'clients.read' },
        loadComponent: () => import('./features/clients/clients').then((m) => m.ClientsComponent),
      },
      {
        path: 'expenses',
        canActivate: [authGuard],
        data: { permission: 'expenses.read' },
        loadComponent: () => import('./features/expenses/expenses').then((m) => m.ExpensesComponent),
      },
      {
        path: 'todos',
        canActivate: [authGuard],
        data: { permission: 'todos.read' },
        loadComponent: () => import('./features/todos/todos').then((m) => m.TodosComponent),
      },
      {
        path: 'users',
        canActivate: [authGuard],
        data: { permission: 'identity.users.read' },
        loadComponent: () => import('./features/users/users').then((m) => m.UsersComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
