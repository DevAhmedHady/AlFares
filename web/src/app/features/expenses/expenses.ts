import { CommonModule } from '@angular/common';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GridComponent } from '../../shared/grid/grid';
import { ModalComponent } from '../../shared/modal/modal';
import { ColumnDef } from '../../shared/grid/grid-column';
import { ExpensesService } from '../../core/api/resources';
import { AuthStore } from '../../core/auth/auth.store';
import { GridFieldType } from '../../core/grid.models';
import { CreateExpenseRequest, ExpenseResponse } from '../../core/models';
import { formatDate, formatMoney } from '../../core/labels';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [CommonModule, FormsModule, GridComponent, ModalComponent],
  templateUrl: './expenses.html',
})
export class ExpensesComponent {
  readonly service = inject(ExpensesService);
  private readonly store = inject(AuthStore);
  private readonly grid = viewChild.required(GridComponent);

  readonly canWrite = this.store.has('expenses.write');
  readonly canDelete = this.store.has('expenses.delete');

  readonly columns: ColumnDef<ExpenseResponse>[] = [
    { key: 'category', header: 'الفئة', type: GridFieldType.Text },
    { key: 'amount', header: 'المبلغ', type: GridFieldType.Number, filterable: false, format: (r) => formatMoney(r.amount) },
    { key: 'date', header: 'التاريخ', type: GridFieldType.Date, format: (r) => formatDate(r.date) },
    { key: 'payee', header: 'المستفيد', type: GridFieldType.Text },
    { key: 'notes', header: 'ملاحظات', type: GridFieldType.Text, filterable: false },
  ];

  readonly editing = signal<ExpenseResponse | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly form = signal<CreateExpenseRequest>(this.blank());

  private blank(): CreateExpenseRequest {
    return { category: '', amount: 0, date: new Date().toISOString().slice(0, 10), payee: '', notes: '' };
  }

  patch<K extends keyof CreateExpenseRequest>(key: K, value: CreateExpenseRequest[K]): void {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  openCreate(): void { this.editing.set(null); this.form.set(this.blank()); this.showForm.set(true); }
  openEdit(row: ExpenseResponse): void {
    this.editing.set(row);
    this.form.set({ category: row.category, amount: row.amount, date: row.date.slice(0, 10), payee: row.payee ?? '', notes: row.notes ?? '' });
    this.showForm.set(true);
  }

  save(): void {
    this.saving.set(true);
    const editing = this.editing();
    const req = editing ? this.service.update(editing.id, this.form()) : this.service.create(this.form());
    req.subscribe({
      next: () => { this.saving.set(false); this.showForm.set(false); this.grid().load(); },
      error: () => this.saving.set(false),
    });
  }

  remove(row: ExpenseResponse): void {
    if (!confirm(`حذف المصروف "${row.category}"؟`)) return;
    this.service.remove(row.id).subscribe(() => this.grid().load());
  }
}
