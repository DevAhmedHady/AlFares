import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { CarsService, ClientsService, RevenuesService } from '../../core/api/resources';
import { ScopedGridSource } from '../../core/api/scoped-source';
import { AuthStore } from '../../core/auth/auth.store';
import {
  dayOptions, monthOptions, validateYmd, yearOptions, ymdDateFilters,
} from '../../core/date-scope';
import { emptyGridQuery, GridFieldType, GridFilter, GridFilterOp } from '../../core/grid.models';
import { formatDate, formatMoney, toDate, toIso } from '../../core/labels';
import { ownerEntityOptions, ownerLinkOptions } from '../../core/owner-link';
import { CarResponse, ClientResponse, CreateRevenueRequest, OwnerType, RevenueResponse, RevenueTypeResponse } from '../../core/models';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';

@Component({
  selector: 'app-revenues',
  standalone: true,
  imports: [
    FormsModule, RouterLink, ButtonModule, DialogModule, InputTextModule, InputNumberModule,
    SelectModule, DatePickerModule, TooltipModule, GridComponent,
  ],
  templateUrl: './revenues.html',
  styleUrl: './revenues.scss',
})
export class RevenuesComponent {
  readonly service = inject(RevenuesService);
  private readonly store = inject(AuthStore);
  private readonly messages = inject(MessageService);
  private readonly grid = viewChild.required(GridComponent);
  private readonly clientsApi = inject(ClientsService);
  private readonly carsApi = inject(CarsService);

  readonly canWrite = this.store.has('revenues.write');
  readonly canDelete = this.store.has('revenues.delete');
  readonly types = signal<RevenueTypeResponse[]>([]);
  readonly from = signal('');
  readonly to = signal('');
  readonly yearFilter = signal<number | null>(null);
  readonly monthFilter = signal<number | null>(null);
  readonly dayFilter = signal<number | null>(null);
  readonly typeFilter = signal('');
  readonly filterError = signal<string | null>(null);
  readonly yearOptions = yearOptions();
  readonly monthOptions = monthOptions;
  readonly dayOptionsList = computed(() => dayOptions(this.yearFilter(), this.monthFilter()));
  readonly usingCalendarScope = computed(() => this.yearFilter() != null);
  readonly hasActiveFilters = computed(() =>
    !!(this.from() || this.to() || this.yearFilter() || this.monthFilter() || this.dayFilter() || this.typeFilter()),
  );
  readonly source = new ScopedGridSource(this.service, () => this.filters());
  readonly show = signal(false);
  readonly saving = signal(false);
  readonly editing = signal<RevenueResponse | null>(null);
  readonly clients = signal<ClientResponse[]>([]);
  readonly cars = signal<CarResponse[]>([]);
  readonly ownerOptions = ownerLinkOptions;
  readonly General = OwnerType.General;
  readonly form = signal<CreateRevenueRequest>({
    revenueTypeId: null, amount: 0, date: new Date().toISOString().slice(0, 10),
    source: '', notes: '', ownerType: OwnerType.General, ownerId: null,
  });
  readonly entityOptions = computed(() => ownerEntityOptions(this.form().ownerType, this.clients(), this.cars()));
  readonly toIso = toIso;
  readonly fromModel = computed(() => toDate(this.from()));
  readonly toModel = computed(() => toDate(this.to()));
  readonly dateModel = computed(() => toDate(this.form().date));
  readonly columns: ColumnDef<RevenueResponse>[] = [
    { key: 'revenueTypeName', header: 'النوع', type: GridFieldType.Text },
    { key: 'amount', header: 'المبلغ', type: GridFieldType.Number, filterable: false, format: (row) => formatMoney(row.amount) },
    { key: 'date', header: 'التاريخ', type: GridFieldType.Date, format: (row) => formatDate(row.date) },
    { key: 'source', header: 'المصدر', type: GridFieldType.Text },
  ];

  constructor() {
    this.service.types().subscribe({ next: (value) => this.types.set(value), error: () => undefined });
    this.clientsApi.grid(emptyGridQuery(500)).subscribe({ next: (p) => this.clients.set(p.items), error: () => undefined });
    this.carsApi.grid(emptyGridQuery(500)).subscribe({ next: (p) => this.cars.set(p.items), error: () => undefined });
  }

  setOwnerType(t: OwnerType): void {
    this.form.update((form) => ({ ...form, ownerType: t, ownerId: null }));
  }

  setYear(value: number | null): void {
    this.yearFilter.set(value);
    if (value == null) {
      this.monthFilter.set(null);
      this.dayFilter.set(null);
    }
  }

  setMonth(value: number | null): void {
    this.monthFilter.set(value);
    if (value == null) this.dayFilter.set(null);
    else {
      const max = dayOptions(this.yearFilter(), value).length;
      const day = this.dayFilter();
      if (day != null && day > max) this.dayFilter.set(null);
    }
  }

  applyFilters(): void {
    if (this.from() && this.to() && this.from() > this.to()) {
      this.filterError.set('يجب أن يكون تاريخ البداية قبل تاريخ النهاية.');
      return;
    }
    const ymdError = validateYmd(this.yearFilter(), this.monthFilter(), this.dayFilter());
    if (ymdError) {
      this.filterError.set(ymdError);
      return;
    }
    this.filterError.set(null);
    this.grid().load();
  }

  clearScopeFilters(): void {
    this.from.set('');
    this.to.set('');
    this.yearFilter.set(null);
    this.monthFilter.set(null);
    this.dayFilter.set(null);
    this.typeFilter.set('');
    this.filterError.set(null);
    this.grid().load();
  }

  patch(key: string, value: unknown): void {
    this.form.update((form) => ({ ...form, [key]: value }));
  }

  valid(): boolean {
    const form = this.form();
    return form.amount > 0 && !!form.date && !!form.source.trim();
  }

  open(): void {
    this.editing.set(null);
    this.form.set({
      revenueTypeId: null, amount: 0, date: new Date().toISOString().slice(0, 10),
      source: '', notes: '', ownerType: OwnerType.General, ownerId: null,
    });
    this.show.set(true);
  }

  edit(row: RevenueResponse): void {
    this.editing.set(row);
    this.form.set({
      revenueTypeId: row.revenueTypeId ?? null, amount: row.amount, date: row.date.slice(0, 10),
      source: row.source, notes: row.notes ?? '', ownerType: row.ownerType, ownerId: row.ownerId ?? null,
    });
    this.show.set(true);
  }

  save(): void {
    this.saving.set(true);
    const row = this.editing();
    const payload = { ...this.form(), revenueTypeId: this.form().revenueTypeId || null };
    (row ? this.service.update(row.id, payload) : this.service.create(payload)).subscribe({
      next: () => {
        this.show.set(false);
        this.grid().load();
        this.messages.add({ severity: 'success', summary: 'تم الحفظ', detail: row ? 'تم تحديث الإيراد' : 'تمت إضافة الإيراد' });
      },
      error: () => this.saving.set(false),
      complete: () => this.saving.set(false),
    });
  }

  remove(row: RevenueResponse): void {
    if (!confirm('حذف هذا الإيراد؟')) return;
    this.service.remove(row.id).subscribe({
      next: () => {
        this.grid().load();
        this.messages.add({ severity: 'success', summary: 'تم الحذف', detail: 'تم حذف الإيراد' });
      },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر الحذف', detail: 'لم يتم حذف الإيراد. أعد المحاولة.' }),
    });
  }

  private filters(): GridFilter[] {
    const filters: GridFilter[] = [];
    if (this.from()) filters.push({ field: 'date', op: GridFilterOp.Gte, value: this.from() });
    if (this.to()) filters.push({ field: 'date', op: GridFilterOp.Lte, value: this.to() });
    filters.push(...ymdDateFilters(this.yearFilter(), this.monthFilter(), this.dayFilter()));
    if (this.typeFilter()) filters.push({ field: 'revenueTypeId', op: GridFilterOp.Eq, value: this.typeFilter() });
    return filters;
  }
}
