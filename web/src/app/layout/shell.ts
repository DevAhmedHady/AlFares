import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthStore } from '../core/auth/auth.store';
import { AuthService } from '../core/auth/auth.service';

interface NavItem { path: string; label: string; icon: string; permission?: string; }

/** App chrome: RTL sidebar navigation (permission-filtered) + content outlet. */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent {
  private readonly store = inject(AuthStore);
  private readonly auth = inject(AuthService);

  readonly email = this.store.email;
  private readonly items: NavItem[] = [
    { path: '/dashboard', label: 'لوحة المعلومات', icon: 'pi pi-chart-pie', permission: 'dashboard.charts.read' },
    { path: '/clients', label: 'العملاء', icon: 'pi pi-users', permission: 'clients.read' },
    { path: '/expenses', label: 'المصروفات', icon: 'pi pi-wallet', permission: 'expenses.read' },
    { path: '/todos', label: 'المهام', icon: 'pi pi-check-square', permission: 'todos.read' },
    { path: '/users', label: 'المستخدمون', icon: 'pi pi-id-card', permission: 'identity.users.read' },
  ];

  readonly nav = computed(() => this.items.filter((i) => this.store.has(i.permission)));

  logout(): void { this.auth.logout(); location.assign('/login'); }
}
