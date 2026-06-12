import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { CarsService, ClientsService, ExpensesService } from '../../core/api/resources';
import { ScopedGridSource } from '../../core/api/scoped-source';
import { AuthStore } from '../../core/auth/auth.store';
import { emptyGridQuery, GridFieldType, GridFilter, GridFilterOp } from '../../core/grid.models';
import { formatDate, formatMoney, toDate, toIso } from '../../core/labels';
import { CarResponse, ClientResponse, CreateExpenseRequest, ExpenseResponse, ExpenseTypeResponse, OwnerType } from '../../core/models';
import { ownerEntityOptions, ownerLinkOptions } from '../../core/owner-link';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [
    CommonModule, FormsModule, GridComponent, DialogModule, ButtonModule, InputTextModule,
    InputNumberModule, TextareaModule, SelectModule, DatePickerModule, TooltipModule,
  ],
  templateUrl: './expenses.html',
})
export class ExpensesComponent {
  readonly service = inject(ExpensesService);
  private readonly store = inject(AuthStore);
  private readonly messages = inject(MessageService);
  private readonly grid = viewChild.required(GridComponent);
  private readonly clientsApi = inject(ClientsService);
  private readonly carsApi = inject(CarsService);

  readonly canWrite = this.store.has('expenses.write');
  readonly canDelete = this.store.has('expenses.delete');
  readonly types = signal<ExpenseTypeResponse[]>([]);
  readonly from = signal('');
  readonly to = signal('');
  readonly typeFilter = signal('');
  readonly filterError = signal<string | null>(null);
  readonly source = new ScopedGridSource(this.service, () => this.filters());
  readonly columns: ColumnDef<ExpenseResponse>[] = [
    { key: 'expenseTypeName', header: 'نوع المصروف', type: GridFieldType.Text },
    { key: 'amount', header: 'المبلغ', type: GridFieldType.Number, filterable: false, format: (row) => formatMoney(row.amount) },
    { key: 'date', header: 'التاريخ', type: GridFieldType.Date, format: (row) => formatDate(row.date) },
    { key: 'payee', header: 'المستفيد', type: GridFieldType.Text },
    { key: 'notes', header: 'ملاحظات', type: GridFieldType.Text, filterable: false },
  ];

  readonly editing = signal<ExpenseResponse | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly clients = signal<ClientResponse[]>([]);
  readonly cars = signal<CarResponse[]>([]);
  readonly ownerOptions = ownerLinkOptions;
  readonly General = OwnerType.General;
  readonly form = signal<CreateExpenseRequest>(this.blank());
  readonly entityOptions = computed(() => ownerEntityOptions(this.form().ownerType, this.clients(), this.cars()));
  readonly toIso = toIso;
  readonly fromModel = computed(() => toDate(this.from()));
  readonly toModel = computed(() => toDate(this.to()));
  readonly dateModel = computed(() => toDate(this.form().date));

  constructor() {
    this.service.types().subscribe((types) => this.types.set(types));
    this.clientsApi.grid(emptyGridQuery(500)).subscribe({ next: (page) => this.clients.set(page.items), error: () => undefined });
    this.carsApi.grid(emptyGridQuery(500)).subscribe({ next: (page) => this.cars.set(page.items), error: () => undefined });
  }

  setOwnerType(type: OwnerType): void {
    this.form.update((form) => ({ ...form, ownerType: type, ownerId: null }));
  }

  applyFilters(): void {
    if (this.from() && this.to() && this.from() > this.to()) {
      this.filterError.set('يجب أن يكون تاريخ البداية قبل تاريخ النهاية.');
      return;
    }
    this.filterError.set(null);
    this.grid().load();
  }

  clearScopeFilters(): void {
    this.from.set('');
    this.to.set('');
    this.typeFilter.set('');
    this.filterError.set(null);
    this.grid().load();
  }

  patch<K extends keyof CreateExpenseRequest>(key: K, value: CreateExpenseRequest[K]): void {
    this.form.update((form) => ({ ...form, [key]: value }));
  }

  openCreate(): void {
    this.editing.set(null);
    this.form.set(this.blank());
    this.formError.set(null);
    this.showForm.set(true);
  }

  openEdit(row: ExpenseResponse): void {
    this.editing.set(row);
    this.form.set({
      expenseTypeId: row.expenseTypeId, amount: row.amount, date: row.date.slice(0, 10),
      payee: row.payee ?? '', notes: row.notes ?? '', ownerType: row.ownerType, ownerId: row.ownerId,
    });
    this.formError.set(null);
    this.showForm.set(true);
  }

  save(): void {
    this.saving.set(true);
    this.formError.set(null);
    const editing = this.editing();
    const request = editing ? this.service.update(editing.id, this.form()) : this.service.create(this.form());
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.grid().load();
        this.messages.add({ severity: 'success', summary: 'تم الحفظ', detail: editing ? 'تم تحديث المصروف' : 'تمت إضافة المصروف' });
      },
      error: (error) => {
        this.saving.set(false);
        this.formError.set(error?.error?.description ?? 'تعذر حفظ المصروف. راجع البيانات ثم أعد المحاولة.');
      },
    });
  }

  remove(row: ExpenseResponse): void {
    if (!confirm(`حذف المصروف "${row.expenseTypeName}"؟`)) return;
    this.service.remove(row.id).subscribe({
      next: () => { this.grid().load(); this.messages.add({ severity: 'success', summary: 'تم الحذف', detail: 'تم حذف المصروف' }); },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر الحذف', detail: 'لم يتم حذف المصروف. أعد المحاولة.' }),
    });
  }

  private filters(): GridFilter[] {
    const filters: GridFilter[] = [];
    if (this.from()) filters.push({ field: 'date', op: GridFilterOp.Gte, value: this.from() });
    if (this.to()) filters.push({ field: 'date', op: GridFilterOp.Lte, value: this.to() });
    if (this.typeFilter()) filters.push({ field: 'expenseTypeId', op: GridFilterOp.Eq, value: this.typeFilter() });
    return filters;
  }

  private blank(): CreateExpenseRequest {
    return { expenseTypeId: '', amount: 0, date: new Date().toISOString().slice(0, 10), payee: '', notes: '', ownerType: OwnerType.General, ownerId: null };
  }
}
