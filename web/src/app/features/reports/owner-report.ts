import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { CarsService, ClientsService, ReportsService } from '../../core/api/resources';
import { emptyGridQuery } from '../../core/grid.models';
import { formatDate, formatMoney, toDate, toIso } from '../../core/labels';
import { CarType, LedgerKind, OwnerLedgerResponse, OwnerType } from '../../core/models';

interface OwnerOption {
  id: string;
  label: string;
}

@Component({
  standalone: true,
  imports: [FormsModule, ButtonModule, DatePickerModule, SelectModule, TagModule],
  template: ` <section class="feature-page">
    <header class="feature-hero">
      <div class="feature-title">
        <span class="feature-icon"><i class="pi pi-book"></i></span>
        <div>
          <h1>كشف حساب</h1>
          <p>تجميع الإيرادات والمصروفات حسب العميل أو السيارة.</p>
        </div>
      </div>
    </header>
    <div class="report-panel">
      <div class="report-filters">
        <div class="field">
          <label for="owner-report-type">نوع الحساب</label
          ><p-select
            class="w-full"
            inputId="owner-report-type"
            [options]="owners"
            optionLabel="label"
            optionValue="value"
            [ngModel]="ownerType()"
            (ngModelChange)="ownerType.set($event)"
            appendTo="body"
          />
        </div>
        <div class="field">
          <label for="owner-report-entity">{{ ownerFieldLabel() }}</label
          ><p-select
            class="w-full"
            inputId="owner-report-entity"
            [options]="entities()"
            optionLabel="label"
            optionValue="id"
            [filter]="true"
            filterBy="label"
            [ngModel]="ownerId()"
            (ngModelChange)="ownerId.set($event)"
            [loading]="loadingEntities()"
            [placeholder]="entityPlaceholder()"
            [emptyMessage]="entityPlaceholder()"
            appendTo="body"
          />
        </div>
        <div class="field">
          <label for="owner-report-from">من تاريخ</label
          ><p-datepicker
            inputId="owner-report-from"
            [ngModel]="fromModel()"
            (ngModelChange)="from.set(toIso($event))"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            iconDisplay="input"
            [showButtonBar]="true"
            appendTo="body"
            styleClass="w-full"
            inputStyleClass="w-full"
          />
        </div>
        <div class="field">
          <label for="owner-report-to">إلى تاريخ</label
          ><p-datepicker
            inputId="owner-report-to"
            [ngModel]="toModel()"
            (ngModelChange)="to.set(toIso($event))"
            dateFormat="dd/mm/yy"
            [showIcon]="true"
            iconDisplay="input"
            [showButtonBar]="true"
            appendTo="body"
            styleClass="w-full"
            inputStyleClass="w-full"
          />
        </div>
      </div>
      <div class="report-action">
        <p-button
          label="عرض التقرير"
          icon="pi pi-search"
          [loading]="loading()"
          [disabled]="!ownerId()"
          (onClick)="load()"
        />
      </div>
    </div>
    @if (data(); as report) {
      <div class="summary-grid">
        <div class="summary-card positive">
          <span>إجمالي الإيرادات</span><strong>{{ money(report.totalRevenues) }}</strong>
        </div>
        <div class="summary-card negative">
          <span>إجمالي المصروفات</span><strong>{{ money(report.totalExpenses) }}</strong>
        </div>
        <div
          class="summary-card"
          [class.positive]="report.net >= 0"
          [class.negative]="report.net < 0"
        >
          <span>صافي الحساب</span><strong>{{ money(report.net) }}</strong>
        </div>
      </div>
      <div class="report-panel">
        @if (report.entries.length) {
          <table class="data-table">
            <thead>
              <tr>
                <th>التاريخ</th>
                <th>الحركة</th>
                <th>البيان</th>
                <th class="num">المبلغ</th>
              </tr>
            </thead>
            <tbody>
              @for (item of report.entries; track item.id) {
                <tr>
                  <td>{{ date(item.date) }}</td>
                  <td>
                    @if (item.kind === Kind.Revenue) {
                      <p-tag severity="success" value="إيراد" />
                    } @else {
                      <p-tag severity="danger" value="مصروف" />
                    }
                  </td>
                  <td>{{ item.description }}</td>
                  <td
                    class="num"
                    [class.amount-in]="item.kind === Kind.Revenue"
                    [class.amount-out]="item.kind !== Kind.Revenue"
                  >
                    {{ item.kind === Kind.Revenue ? '+' : '−' }} {{ money(item.amount) }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        } @else {
          <div class="empty-panel">
            <i class="pi pi-inbox"></i>
            <p>لا توجد حركات ضمن الفترة المحددة.</p>
          </div>
        }
      </div>
    } @else if (loaded()) {
      <div class="report-panel empty-panel">
        <i class="pi pi-search"></i>
        <p>اختر حسابًا ثم اعرض التقرير.</p>
      </div>
    }
  </section>`,
})
export class OwnerReportComponent {
  private readonly reports = inject(ReportsService);
  private readonly clients = inject(ClientsService);
  private readonly cars = inject(CarsService);
  readonly Kind = LedgerKind;
  readonly ownerType = signal(OwnerType.Client);
  readonly ownerId = signal('');
  readonly from = signal('');
  readonly to = signal('');
  readonly data = signal<OwnerLedgerResponse | null>(null);
  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly entities = signal<OwnerOption[]>([]);
  readonly loadingEntities = signal(false);
  readonly money = formatMoney;
  readonly date = formatDate;
  readonly toIso = toIso;
  readonly fromModel = computed(() => toDate(this.from()));
  readonly toModel = computed(() => toDate(this.to()));
  readonly owners = [
    { label: 'عميل', value: OwnerType.Client },
    { label: 'سيارة مملوكة', value: OwnerType.OwnedCar },
    { label: 'سيارة مؤجرة', value: OwnerType.RentedCar },
  ];

  readonly ownerFieldLabel = computed(() =>
    this.ownerType() === OwnerType.Client ? 'العميل' : 'السيارة',
  );
  readonly entityPlaceholder = computed(() =>
    this.loadingEntities()
      ? 'جارٍ التحميل...'
      : this.entities().length
        ? `اختر ${this.ownerFieldLabel()}`
        : `لا توجد ${this.ownerType() === OwnerType.Client ? 'عملاء' : 'سيارات'}`,
  );

  constructor() {
    // Reload the picker whenever the account type changes; clear any stale selection + report.
    effect(() => {
      this.ownerType();
      this.ownerId.set('');
      this.data.set(null);
      this.loaded.set(false);
      this.loadEntities();
    });
  }

  private loadEntities(): void {
    const type = this.ownerType();
    this.loadingEntities.set(true);
    if (type === OwnerType.Client) {
      this.clients.grid(emptyGridQuery(500)).subscribe({
        next: (page) => this.entities.set(page.items.map((c) => ({ id: c.id, label: c.name }))),
        error: () => this.entities.set([]),
        complete: () => this.loadingEntities.set(false),
      });
    } else {
      const carType = type === OwnerType.OwnedCar ? CarType.Owned : CarType.Rented;
      this.cars.grid(emptyGridQuery(500)).subscribe({
        next: (page) =>
          this.entities.set(
            page.items
              .filter((c) => c.type === carType)
              .map((c) => ({
                id: c.id,
                label: c.plateNumber ? `${c.name} · ${c.plateNumber}` : c.name,
              })),
          ),
        error: () => this.entities.set([]),
        complete: () => this.loadingEntities.set(false),
      });
    }
  }

  load(): void {
    if (!this.ownerId()) return;
    this.loading.set(true);
    this.loaded.set(true);
    this.reports.ledger(this.ownerType(), this.ownerId(), this.from(), this.to()).subscribe({
      next: (value) => this.data.set(value),
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
  }
}
