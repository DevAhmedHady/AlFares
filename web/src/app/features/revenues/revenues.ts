import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { CarsService, ClientsService, RevenuesService } from '../../core/api/resources';
import { emptyGridQuery, GridFieldType } from '../../core/grid.models';
import { formatDate, formatMoney, toDate, toIso } from '../../core/labels';
import { ownerEntityOptions, ownerLinkOptions } from '../../core/owner-link';
import { CarResponse, ClientResponse, OwnerType, RevenueResponse, RevenueTypeResponse } from '../../core/models';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';

@Component({ standalone: true, imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, DatePickerModule, TooltipModule, GridComponent], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-chart-line"></i></span><div><h1>الإيرادات</h1><p>تسجيل ومراجعة مصادر الدخل اليومية للمصنع.</p></div></div></header>
  <app-grid title="سجل الإيرادات" exportName="revenues" [columns]="columns" [source]="service" [rowActions]="actions" createPermission="revenues.write" exportPermission="revenues.export" (createClicked)="open()"/></section>
  <ng-template #actions let-row><div class="row-actions"><p-button icon="pi pi-pencil" [rounded]="true" [text]="true" pTooltip="تعديل" (onClick)="edit(row)"/><p-button icon="pi pi-trash" severity="danger" [rounded]="true" [text]="true" pTooltip="حذف" (onClick)="remove(row)"/></div></ng-template>
  <p-dialog [visible]="show()" (visibleChange)="show.set($event)" [modal]="true" [draggable]="false" [style]="{width:'min(620px, 94vw)'}" [header]="editing() ? 'تعديل الإيراد' : 'إضافة إيراد'">
    <div class="form-grid"><div class="field"><label>نوع الإيراد</label><p-select [options]="types()" optionLabel="name" optionValue="id" placeholder="اختر النوع" [ngModel]="form().revenueTypeId" (ngModelChange)="patch('revenueTypeId',$event)"/></div><div class="field"><label>المبلغ</label><p-inputnumber mode="decimal" [min]="0" [minFractionDigits]="2" [ngModel]="form().amount" (ngModelChange)="patch('amount',$event||0)"/></div><div class="field"><label>التاريخ</label><p-datepicker [ngModel]="dateModel()" (ngModelChange)="patch('date',toIso($event))" dateFormat="dd/mm/yy" [showIcon]="true" iconDisplay="input" appendTo="body" styleClass="w-full" inputStyleClass="w-full"/></div><div class="field"><label>المصدر</label><input pInputText [ngModel]="form().source" (ngModelChange)="patch('source',$event)" placeholder="مثال: مبيعات الطوب"/></div><div class="field"><label>ربط بحساب</label><p-select [options]="ownerOptions" optionLabel="label" optionValue="value" [ngModel]="form().ownerType" (ngModelChange)="setOwnerType($event)"/></div>@if(form().ownerType!==General){<div class="field"><label>الحساب</label><p-select [options]="entityOptions()" optionLabel="label" optionValue="id" [filter]="true" filterBy="label" [ngModel]="form().ownerId" (ngModelChange)="patch('ownerId',$event)" placeholder="اختر الحساب"/></div>}<div class="field span-2"><label>ملاحظات</label><textarea pInputText rows="3" [ngModel]="form().notes" (ngModelChange)="patch('notes',$event)"></textarea></div></div>
    <ng-template pTemplate="footer"><div class="dialog-actions"><p-button label="إلغاء" severity="secondary" [text]="true" (onClick)="show.set(false)"/><p-button label="حفظ" icon="pi pi-check" [loading]="saving()" [disabled]="!valid()" (onClick)="save()"/></div></ng-template>
  </p-dialog>` })
export class RevenuesComponent {
  readonly service = inject(RevenuesService); private readonly grid = viewChild.required(GridComponent);
  private readonly clientsApi = inject(ClientsService); private readonly carsApi = inject(CarsService);
  readonly types = signal<RevenueTypeResponse[]>([]); readonly show = signal(false); readonly saving = signal(false); readonly editing = signal<RevenueResponse | null>(null);
  readonly clients = signal<ClientResponse[]>([]); readonly cars = signal<CarResponse[]>([]); readonly ownerOptions = ownerLinkOptions; readonly General = OwnerType.General;
  readonly form = signal({ revenueTypeId: '', amount: 0, date: new Date().toISOString().slice(0, 10), source: '', notes: '', ownerType: OwnerType.General, ownerId: null as string | null });
  readonly entityOptions = computed(() => ownerEntityOptions(this.form().ownerType, this.clients(), this.cars()));
  readonly toIso = toIso; readonly dateModel = computed(() => toDate(this.form().date));
  readonly columns: ColumnDef<RevenueResponse>[] = [{ key: 'revenueTypeName', header: 'النوع', type: GridFieldType.Text }, { key: 'amount', header: 'المبلغ', type: GridFieldType.Number, format: row => formatMoney(row.amount) }, { key: 'date', header: 'التاريخ', type: GridFieldType.Date, format: row => formatDate(row.date) }, { key: 'source', header: 'المصدر', type: GridFieldType.Text }];
  constructor() {
    this.service.types().subscribe({ next: value => this.types.set(value), error: () => undefined });
    this.clientsApi.grid(emptyGridQuery(500)).subscribe({ next: p => this.clients.set(p.items), error: () => undefined });
    this.carsApi.grid(emptyGridQuery(500)).subscribe({ next: p => this.cars.set(p.items), error: () => undefined });
  }
  setOwnerType(t: OwnerType): void { this.form.update(form => ({ ...form, ownerType: t, ownerId: null })); }
  patch(key: string, value: unknown): void { this.form.update(form => ({ ...form, [key]: value })); }
  valid(): boolean { const form = this.form(); return !!form.revenueTypeId && form.amount > 0 && !!form.date && !!form.source.trim(); }
  open(): void { this.editing.set(null); this.form.set({ revenueTypeId: '', amount: 0, date: new Date().toISOString().slice(0, 10), source: '', notes: '', ownerType: OwnerType.General, ownerId: null }); this.show.set(true); }
  edit(row: RevenueResponse): void { this.editing.set(row); this.form.set({ revenueTypeId: row.revenueTypeId, amount: row.amount, date: row.date.slice(0, 10), source: row.source, notes: row.notes ?? '', ownerType: row.ownerType, ownerId: row.ownerId ?? null }); this.show.set(true); }
  save(): void { this.saving.set(true); const row = this.editing(); (row ? this.service.update(row.id, this.form()) : this.service.create(this.form())).subscribe({ next: () => { this.show.set(false); this.grid().load(); }, error: () => this.saving.set(false), complete: () => this.saving.set(false) }); }
  remove(row: RevenueResponse): void { if (confirm('حذف هذا الإيراد؟')) this.service.remove(row.id).subscribe({ next: () => this.grid().load(), error: () => undefined }); }
}
