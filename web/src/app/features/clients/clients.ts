import { CommonModule } from '@angular/common';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GridComponent } from '../../shared/grid/grid';
import { ModalComponent } from '../../shared/modal/modal';
import { ColumnDef } from '../../shared/grid/grid-column';
import { ClientsService } from '../../core/api/resources';
import { AuthStore } from '../../core/auth/auth.store';
import { GridFieldType } from '../../core/grid.models';
import { ActivityLevel, ClientResponse, ClientStatus, CreateClientRequest } from '../../core/models';
import {
  activityLabels, clientStatusLabels, formatDate, formatMoney, optionsFrom,
} from '../../core/labels';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, FormsModule, GridComponent, ModalComponent],
  templateUrl: './clients.html',
})
export class ClientsComponent {
  readonly service = inject(ClientsService);
  private readonly store = inject(AuthStore);
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
    { key: 'accountBalance', header: 'الرصيد', type: GridFieldType.Number, filterable: false, format: (r) => formatMoney(r.accountBalance) },
    { key: 'activityLevel', header: 'النشاط', type: GridFieldType.Enum, options: optionsFrom(activityLabels), format: (r) => activityLabels[r.activityLevel] },
    { key: 'status', header: 'الحالة', type: GridFieldType.Enum, options: optionsFrom(clientStatusLabels), format: (r) => clientStatusLabels[r.status] },
    { key: 'createdAt', header: 'تاريخ الإنشاء', type: GridFieldType.Date, filterable: false, format: (r) => formatDate(r.createdAtUtc) },
  ];

  readonly editing = signal<ClientResponse | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly form = signal<CreateClientRequest>(this.blank());

  private blank(): CreateClientRequest {
    return { name: '', contactName: '', phone: '', email: '', accountBalance: 0, activityLevel: ActivityLevel.Medium, notes: '' };
  }

  patch<K extends keyof CreateClientRequest>(key: K, value: CreateClientRequest[K]): void {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  openCreate(): void { this.editing.set(null); this.form.set(this.blank()); this.showForm.set(true); }

  openEdit(row: ClientResponse): void {
    this.editing.set(row);
    this.form.set({
      name: row.name, contactName: row.contactName, phone: row.phone ?? '', email: row.email ?? '',
      accountBalance: row.accountBalance, activityLevel: row.activityLevel, notes: row.notes ?? '',
    });
    this.showForm.set(true);
  }

  save(): void {
    this.saving.set(true);
    const body = this.form();
    const editing = this.editing();
    const req = editing ? this.service.update(editing.id, body) : this.service.create(body);
    req.subscribe({
      next: () => { this.saving.set(false); this.showForm.set(false); this.grid().load(); },
      error: () => this.saving.set(false),
    });
  }

  remove(row: ClientResponse): void {
    if (!confirm(`حذف العميل "${row.name}"؟`)) return;
    this.service.remove(row.id).subscribe(() => this.grid().load());
  }

  toggleStatus(row: ClientResponse): void {
    const next = row.status === ClientStatus.Active ? ClientStatus.Inactive : ClientStatus.Active;
    this.service.setStatus(row.id, next).subscribe(() => this.grid().load());
  }
}
