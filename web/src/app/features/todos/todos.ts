import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';
import { TodosService } from '../../core/api/resources';
import { AuthStore } from '../../core/auth/auth.store';
import { emptyGridQuery, GridFieldType, GridFilterOp } from '../../core/grid.models';
import { CreateTodoRequest, TodoPriority, TodoResponse, TodoStatus } from '../../core/models';
import { formatDate, optionsFrom, todoPriorityLabels, todoStatusLabels, toDate, toIso } from '../../core/labels';

@Component({
  selector: 'app-todos',
  standalone: true,
  imports: [
    CommonModule, FormsModule, GridComponent, DialogModule, ButtonModule,
    InputTextModule, SelectModule, DatePickerModule, TextareaModule, TooltipModule,
  ],
  templateUrl: './todos.html',
})
export class TodosComponent {
  readonly service = inject(TodosService);
  private readonly store = inject(AuthStore);
  private readonly messages = inject(MessageService);
  private readonly grid = viewChild.required(GridComponent);

  readonly canWrite = this.store.has('todos.write');
  readonly canDelete = this.store.has('todos.delete');
  readonly priorityOptions = optionsFrom(todoPriorityLabels);
  readonly todoPriorityLabels = todoPriorityLabels;
  readonly statusOptions = optionsFrom(todoStatusLabels);
  readonly today = new Date().toISOString().slice(0, 10);
  readonly todayDate = new Date();
  readonly toIso = toIso;
  readonly dueDateModel = computed(() => toDate(this.form().dueDate));
  readonly todayTasks = signal<TodoResponse[]>([]);

  readonly columns: ColumnDef<TodoResponse>[] = [
    { key: 'title', header: 'العنوان', type: GridFieldType.Text },
    { key: 'dueDate', header: 'تاريخ الاستحقاق', type: GridFieldType.Date, format: (r) => formatDate(r.dueDate) },
    { key: 'dueTime', header: 'وقت الاستحقاق', type: GridFieldType.Text, filterable: false },
    { key: 'status', header: 'الحالة', type: GridFieldType.Enum, options: optionsFrom(todoStatusLabels), format: (r) => todoStatusLabels[r.status] },
    { key: 'priority', header: 'الأولوية', type: GridFieldType.Enum, options: optionsFrom(todoPriorityLabels), format: (r) => todoPriorityLabels[r.priority] },
    { key: 'notes', header: 'ملاحظات', type: GridFieldType.Text, filterable: false },
  ];

  readonly editing = signal<TodoResponse | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly form = signal<CreateTodoRequest>(this.blank());

  private blank(): CreateTodoRequest {
    return { title: '', dueDate: this.today, dueTime: null, priority: TodoPriority.Normal, notes: '' };
  }

  patch<K extends keyof CreateTodoRequest>(key: K, value: CreateTodoRequest[K]): void {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  openCreate(): void { this.editing.set(null); this.form.set(this.blank()); this.formError.set(null); this.showForm.set(true); }
  openEdit(row: TodoResponse): void {
    this.editing.set(row);
    this.form.set({ title: row.title, dueDate: row.dueDate.slice(0, 10), dueTime: row.dueTime ?? null, priority: row.priority, notes: row.notes ?? '' });
    this.formError.set(null);
    this.showForm.set(true);
  }

  save(): void {
    this.saving.set(true);
    this.formError.set(null);
    const editing = this.editing();
    const req = editing ? this.service.update(editing.id, this.form()) : this.service.create(this.form());
    req.subscribe({
      next: () => {
        this.saving.set(false); this.showForm.set(false); this.refresh();
        this.messages.add({ severity: 'success', summary: 'تم الحفظ', detail: editing ? 'تم تحديث المهمة' : 'تمت إضافة المهمة' });
      },
      error: (error) => {
        this.saving.set(false);
        this.formError.set(error?.error?.description ?? 'تعذر حفظ المهمة. راجع البيانات ثم أعد المحاولة.');
      },
    });
  }

  cycleStatus(row: TodoResponse): void {
    const next = (row.status + 1) % 3 as TodoStatus;
    this.service.changeStatus(row.id, next).subscribe({
      next: () => {
        this.refresh();
        this.messages.add({ severity: 'success', summary: 'تم تحديث الحالة', detail: `الحالة الجديدة: ${todoStatusLabels[next]}` });
      },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر تحديث الحالة', detail: 'أعد المحاولة بعد قليل.' }),
    });
  }

  remove(row: TodoResponse): void {
    if (!confirm(`حذف المهمة "${row.title}"؟`)) return;
    this.service.remove(row.id).subscribe({
      next: () => { this.refresh(); this.messages.add({ severity: 'success', summary: 'تم الحذف', detail: `تم حذف المهمة ${row.title}` }); },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر الحذف', detail: 'لم يتم حذف المهمة. أعد المحاولة.' }),
    });
  }

  constructor() {
    this.loadTodayTasks();
  }

  private refresh(): void {
    this.grid().load();
    this.loadTodayTasks();
  }

  private loadTodayTasks(): void {
    const query = emptyGridQuery(100);
    query.filters = [{ field: 'dueDate', op: GridFilterOp.Eq, value: this.today }];
    this.service.grid(query).subscribe({
      next: (result) => this.todayTasks.set(result.items),
      error: () => this.messages.add({ severity: 'warn', summary: 'تعذر تحديث مهام اليوم', detail: 'يمكنك متابعة جميع المهام من الجدول.' }),
    });
  }
}
