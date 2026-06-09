import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthStore } from '../core/auth/auth.store';
import { AuthService } from '../core/auth/auth.service';

interface NavItem { path: string; label: string; permission?: string; }

/** App chrome: RTL sidebar navigation (permission-filtered) + content outlet. */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent {
  private readonly store = inject(AuthStore);
  private readonly auth = inject(AuthService);

  readonly email = this.store.email;
  private readonly items: NavItem[] = [
    { path: '/dashboard', label: 'لوحة المعلومات', permission: 'dashboard.charts.read' },
    { path: '/clients', label: 'العملاء', permission: 'clients.read' },
    { path: '/expenses', label: 'المصروفات', permission: 'expenses.read' },
    { path: '/todos', label: 'المهام', permission: 'todos.read' },
    { path: '/users', label: 'المستخدمون', permission: 'identity.users.read' },
  ];

  readonly nav = computed(() => this.items.filter((i) => this.store.has(i.permission)));

  logout(): void { this.auth.logout(); location.assign('/login'); }
}
