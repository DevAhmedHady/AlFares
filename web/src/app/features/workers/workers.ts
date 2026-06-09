import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { ExpensesService, WorkersService } from '../../core/api/resources';
import { GridFieldType } from '../../core/grid.models';
import { formatMoney } from '../../core/labels';
import { ExpenseScope, ExpenseTypeResponse, WorkerResponse } from '../../core/models';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';

@Component({ standalone: true, imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, TooltipModule, GridComponent], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-users"></i></span><div><h1>العمال</h1><p>إدارة العمال ومتابعة السلف والتسويات والرصيد الحالي.</p></div></div></header>
  <app-grid title="سجل العمال" exportName="workers" [columns]="columns" [source]="service" [rowActions]="actions" createPermission="workers.write" exportPermission="workers.export" (createClicked)="openWorker()"/></section>
  <ng-template #actions let-row><div class="row-actions"><p-button label="سلفة" icon="pi pi-arrow-down" size="small" [text]="true" (onClick)="openTxn(row,'advance')"/><p-button label="تسوية" icon="pi pi-check-circle" size="small" [text]="true" severity="success" (onClick)="openTxn(row,'settle')"/></div></ng-template>
  <p-dialog [visible]="workerDialog()" (visibleChange)="workerDialog.set($event)" [modal]="true" [draggable]="false" [style]="{width:'min(540px, 94vw)'}" header="إضافة عامل"><div class="form-grid"><div class="field span-2"><label>اسم العامل</label><input pInputText [ngModel]="workerForm().name" (ngModelChange)="workerForm.update(x=>({...x,name:$event}))"/></div><div class="field span-2"><label>المسمى الوظيفي</label><input pInputText [ngModel]="workerForm().jobTitle" (ngModelChange)="workerForm.update(x=>({...x,jobTitle:$event}))"/></div></div><ng-template pTemplate="footer"><div class="dialog-actions"><p-button label="إلغاء" severity="secondary" [text]="true" (onClick)="workerDialog.set(false)"/><p-button label="حفظ" icon="pi pi-check" [loading]="saving()" [disabled]="!workerForm().name.trim()" (onClick)="saveWorker()"/></div></ng-template></p-dialog>
  <p-dialog [visible]="txnDialog()" (visibleChange)="txnDialog.set($event)" [modal]="true" [draggable]="false" [style]="{width:'min(560px, 94vw)'}" [header]="mode()==='advance' ? 'تسجيل سلفة' : 'تسجيل تسوية'"><p class="muted">العامل: <strong>{{selected()?.name}}</strong></p><div class="form-grid"><div class="field"><label>المبلغ</label><p-inputnumber [min]="0" [minFractionDigits]="2" [ngModel]="txn().amount" (ngModelChange)="txn.update(x=>({...x,amount:$event||0}))"/></div><div class="field"><label>التاريخ</label><input pInputText type="date" [ngModel]="txn().date" (ngModelChange)="txn.update(x=>({...x,date:$event}))"/></div>@if(mode()==='advance'){<div class="field span-2"><label>نوع المصروف</label><p-select [options]="types()" optionLabel="name" optionValue="id" placeholder="اختر النوع" [ngModel]="txn().expenseTypeId" (ngModelChange)="txn.update(x=>({...x,expenseTypeId:$event}))"/></div>}<div class="field span-2"><label>ملاحظات</label><textarea pInputText rows="3" [ngModel]="txn().notes" (ngModelChange)="txn.update(x=>({...x,notes:$event}))"></textarea></div></div><ng-template pTemplate="footer"><div class="dialog-actions"><p-button label="إلغاء" severity="secondary" [text]="true" (onClick)="txnDialog.set(false)"/><p-button label="حفظ" icon="pi pi-check" [loading]="saving()" [disabled]="!validTxn()" (onClick)="saveTxn()"/></div></ng-template></p-dialog>` })
export class WorkersComponent {
  readonly service = inject(WorkersService); private readonly expenses = inject(ExpensesService); private readonly grid = viewChild.required(GridComponent);
  readonly columns: ColumnDef<WorkerResponse>[] = [{ key: 'name', header: 'الاسم', type: GridFieldType.Text }, { key: 'jobTitle', header: 'المسمى الوظيفي', type: GridFieldType.Text }, { key: 'balance', header: 'رصيد السلف', type: GridFieldType.Number, format: row => formatMoney(row.balance) }];
  readonly workerDialog = signal(false); readonly txnDialog = signal(false); readonly saving = signal(false); readonly selected = signal<WorkerResponse | null>(null); readonly mode = signal<'advance' | 'settle'>('advance'); readonly types = signal<ExpenseTypeResponse[]>([]);
  readonly workerForm = signal({ name: '', jobTitle: '', isActive: true }); readonly txn = signal({ amount: 0, date: new Date().toISOString().slice(0, 10), notes: '', expenseTypeId: '' });
  constructor() { this.expenses.types(ExpenseScope.General).subscribe({ next: value => this.types.set(value), error: () => undefined }); }
  openWorker(): void { this.workerForm.set({ name: '', jobTitle: '', isActive: true }); this.workerDialog.set(true); }
  saveWorker(): void { this.saving.set(true); this.service.create(this.workerForm()).subscribe({ next: () => { this.workerDialog.set(false); this.grid().load(); }, error: () => this.saving.set(false), complete: () => this.saving.set(false) }); }
  openTxn(row: WorkerResponse, mode: 'advance' | 'settle'): void { this.selected.set(row); this.mode.set(mode); this.txn.set({ amount: 0, date: new Date().toISOString().slice(0, 10), notes: '', expenseTypeId: '' }); this.txnDialog.set(true); }
  validTxn(): boolean { const value = this.txn(); return value.amount > 0 && !!value.date && (this.mode() === 'settle' || !!value.expenseTypeId); }
  saveTxn(): void { this.saving.set(true); const worker = this.selected()!; const request = this.mode() === 'advance' ? this.service.advance(worker.id, this.txn()) : this.service.settle(worker.id, this.txn()); request.subscribe({ next: () => { this.txnDialog.set(false); this.grid().load(); }, error: () => this.saving.set(false), complete: () => this.saving.set(false) }); }
}
