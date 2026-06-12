import { CommonModule } from '@angular/common';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';
import { ClientsService, ReportsService } from '../../core/api/resources';
import { catchError, map, of, switchMap } from 'rxjs';
import { AuthStore } from '../../core/auth/auth.store';
import { GridFieldType } from '../../core/grid.models';
import { ActivityLevel, ClientResponse, ClientStatus, CreateClientRequest, OwnerType } from '../../core/models';
import {
  activityLabels, clientStatusLabels, formatDate, formatMoney, optionsFrom,
} from '../../core/labels';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [
    CommonModule, FormsModule, GridComponent, DialogModule, ButtonModule,
    InputTextModule, InputNumberModule, SelectModule, TextareaModule, TooltipModule,
  ],
  templateUrl: './clients.html',
})
export class ClientsComponent {
  readonly service = inject(ClientsService);
  private readonly reports = inject(ReportsService);
  private readonly store = inject(AuthStore);
  private readonly messages = inject(MessageService);
  private readonly grid = viewChild.required(GridComponent);

  readonly canWrite = this.store.has('clients.write');
  readonly canDelete = this.store.has('clients.delete');

  readonly Activity = ActivityLevel;
  readonly Status = ClientStatus;
  readonly activityOptions = optionsFrom(activityLabels);

  readonly columns: ColumnDef<ClientResponse>[] = [
    { key: 'name', header: 'اسم العميل', type: GridFieldType.Text },
    { key: 'contactName', header: 'جهة الاتصال', type: GridFieldType.Text },
    { key: 'phone', header: 'الهاتف', type: GridFieldType.Text },
    { key: 'email', header: 'البريد', type: GridFieldType.Text },
    { key: 'accountBalance', header: 'الرصيد الحالي', type: GridFieldType.Number, filterable: false, format: (r) => formatMoney(r.displayBalance ?? r.accountBalance) },
    { key: 'activityLevel', header: 'النشاط', type: GridFieldType.Enum, options: optionsFrom(activityLabels), format: (r) => activityLabels[r.activityLevel] },
    { key: 'status', header: 'الحالة', type: GridFieldType.Enum, options: optionsFrom(clientStatusLabels), format: (r) => clientStatusLabels[r.status] },
    { key: 'createdAt', header: 'تاريخ الإنشاء', type: GridFieldType.Date, filterable: false, format: (r) => formatDate(r.createdAtUtc) },
  ];

  readonly editing = signal<ClientResponse | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly form = signal<CreateClientRequest>(this.blank());
  readonly source = {
    grid: (query: import('../../core/grid.models').GridQuery) => this.service.grid(query).pipe(
      switchMap((page) => this.reports.balances(OwnerType.Client, page.items.map((x) => x.id)).pipe(
        map((balances) => ({ ...page, items: page.items.map((x) => ({ ...x, displayBalance: x.accountBalance + (balances[x.id]?.net ?? 0) })) })),
        // Never let a reports outage blank the clients grid — fall back to the base balances.
        catchError(() => of(page)),
      ))),
    export: (format: import('../../core/grid.models').ExportFormat, query: import('../../core/grid.models').GridQuery) => this.service.export(format, query),
  };

  private blank(): CreateClientRequest {
    return { name: '', contactName: '', phone: '', email: '', accountBalance: 0, activityLevel: ActivityLevel.Medium, notes: '' };
  }

  patch<K extends keyof CreateClientRequest>(key: K, value: CreateClientRequest[K]): void {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  openCreate(): void { this.editing.set(null); this.form.set(this.blank()); this.formError.set(null); this.showForm.set(true); }

  openEdit(row: ClientResponse): void {
    this.editing.set(row);
    this.form.set({
      name: row.name, contactName: row.contactName, phone: row.phone ?? '', email: row.email ?? '',
      accountBalance: row.accountBalance, activityLevel: row.activityLevel, notes: row.notes ?? '',
    });
    this.formError.set(null);
    this.showForm.set(true);
  }

  save(): void {
    this.saving.set(true);
    this.formError.set(null);
    const body = this.form();
    const editing = this.editing();
    const req = editing ? this.service.update(editing.id, body) : this.service.create(body);
    req.subscribe({
      next: () => {
        this.saving.set(false); this.showForm.set(false); this.grid().load();
        this.messages.add({ severity: 'success', summary: 'تم الحفظ', detail: editing ? 'تم تحديث بيانات العميل' : 'تمت إضافة العميل' });
      },
      error: (error) => {
        this.saving.set(false);
        this.formError.set(error?.error?.description ?? 'تعذر حفظ بيانات العميل. راجع الحقول ثم أعد المحاولة.');
      },
    });
  }

  remove(row: ClientResponse): void {
    if (!confirm(`حذف العميل "${row.name}"؟`)) return;
    this.service.remove(row.id).subscribe({
      next: () => { this.grid().load(); this.messages.add({ severity: 'success', summary: 'تم الحذف', detail: `تم حذف العميل ${row.name}` }); },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر الحذف', detail: 'لم يتم حذف العميل. أعد المحاولة.' }),
    });
  }

  toggleStatus(row: ClientResponse): void {
    const next = row.status === ClientStatus.Active ? ClientStatus.Inactive : ClientStatus.Active;
    this.service.setStatus(row.id, next).subscribe({
      next: () => {
        this.grid().load();
        this.messages.add({ severity: 'success', summary: 'تم تحديث الحالة', detail: `الحالة الجديدة: ${clientStatusLabels[next]}` });
      },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر تحديث الحالة', detail: 'أعد المحاولة بعد قليل.' }),
    });
  }
}
