import { CommonModule } from '@angular/common';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';
import { TodosService } from '../../core/api/resources';
import { AuthStore } from '../../core/auth/auth.store';
import { GridFieldType } from '../../core/grid.models';
import { CreateTodoRequest, TodoPriority, TodoResponse, TodoStatus } from '../../core/models';
import { formatDate, optionsFrom, todoPriorityLabels, todoStatusLabels } from '../../core/labels';

@Component({
  selector: 'app-todos',
  standalone: true,
  imports: [
    CommonModule, FormsModule, GridComponent, DialogModule, ButtonModule,
    InputTextModule, SelectModule, TextareaModule,
  ],
  templateUrl: './todos.html',
})
export class TodosComponent {
  readonly service = inject(TodosService);
  private readonly store = inject(AuthStore);
  private readonly grid = viewChild.required(GridComponent);

  readonly canWrite = this.store.has('todos.write');
  readonly canDelete = this.store.has('todos.delete');
  readonly priorityOptions = optionsFrom(todoPriorityLabels);
  readonly statusOptions = optionsFrom(todoStatusLabels);
  readonly today = new Date().toISOString().slice(0, 10);

  readonly columns: ColumnDef<TodoResponse>[] = [
    { key: 'title', header: 'العنوان', type: GridFieldType.Text },
    { key: 'dueDate', header: 'تاريخ الاستحقاق', type: GridFieldType.Date, format: (r) => formatDate(r.dueDate) },
    { key: 'status', header: 'الحالة', type: GridFieldType.Enum, options: optionsFrom(todoStatusLabels), format: (r) => todoStatusLabels[r.status] },
    { key: 'priority', header: 'الأولوية', type: GridFieldType.Enum, options: optionsFrom(todoPriorityLabels), format: (r) => todoPriorityLabels[r.priority] },
    { key: 'notes', header: 'ملاحظات', type: GridFieldType.Text, filterable: false },
  ];

  readonly editing = signal<TodoResponse | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly form = signal<CreateTodoRequest>(this.blank());

  private blank(): CreateTodoRequest {
    return { title: '', dueDate: this.today, priority: TodoPriority.Normal, notes: '' };
  }

  patch<K extends keyof CreateTodoRequest>(key: K, value: CreateTodoRequest[K]): void {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  openCreate(): void { this.editing.set(null); this.form.set(this.blank()); this.showForm.set(true); }
  openEdit(row: TodoResponse): void {
    this.editing.set(row);
    this.form.set({ title: row.title, dueDate: row.dueDate.slice(0, 10), priority: row.priority, notes: row.notes ?? '' });
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

  cycleStatus(row: TodoResponse): void {
    const next = (row.status + 1) % 3 as TodoStatus;
    this.service.changeStatus(row.id, next).subscribe(() => this.grid().load());
  }

  remove(row: TodoResponse): void {
    if (!confirm(`حذف المهمة "${row.title}"؟`)) return;
    this.service.remove(row.id).subscribe(() => this.grid().load());
  }
}
