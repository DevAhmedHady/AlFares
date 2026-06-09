import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TenantInfo } from '../../core/api/tenant.service';

/** Single-tenant login: the tenant is auto-resolved; the user supplies email + password. */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly email = signal('admin@alfaris.local');
  readonly password = signal('');
  readonly tenant = signal<TenantInfo | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  constructor() {
    this.auth.defaultTenant().subscribe({
      next: (t) => this.tenant.set(t),
      error: () => this.error.set('تعذر العثور على المؤسسة. تأكد من تشغيل الخادم.'),
    });
  }

  submit(): void {
    const tenant = this.tenant();
    if (!tenant || this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.auth.login({ email: this.email(), password: this.password(), tenantId: tenant.id }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (e) => {
        this.error.set(e?.error?.description ?? 'فشل تسجيل الدخول. تحقق من البيانات.');
        this.busy.set(false);
      },
    });
  }
}
