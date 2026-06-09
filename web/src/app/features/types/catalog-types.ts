import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { API_BASE } from '../../core/config';
import { expenseScopeLabels, optionsFrom } from '../../core/labels';
import { ExpenseScope, ExpenseTypeResponse, RevenueTypeResponse } from '../../core/models';

type CatalogType = ExpenseTypeResponse | RevenueTypeResponse;

@Component({ standalone: true, imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, SelectModule, TagModule], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-tags"></i></span><div><h1>{{expense ? 'أنواع المصروفات' : 'أنواع الإيرادات'}}</h1><p>إدارة التصنيفات المستخدمة في التسجيل والتقارير.</p></div></div><p-button label="إضافة نوع" icon="pi pi-plus" (onClick)="open()"/></header>
  <div class="report-panel">@if(items().length){<table class="data-table"><thead><tr><th>الاسم</th>@if(expense){<th>النطاق</th>}<th>الحالة</th><th>إجراءات</th></tr></thead><tbody>@for(item of items();track item.id){<tr><td><strong>{{item.name}}</strong></td>@if(expense){<td>{{scopeLabel(item)}}</td>}<td><p-tag [severity]="item.isActive?'success':'secondary'" [value]="item.isActive?'نشط':'متوقف'"/></td><td><div class="row-actions"><p-button icon="pi pi-pencil" [rounded]="true" [text]="true" (onClick)="edit(item)"/><p-button icon="pi pi-trash" severity="danger" [rounded]="true" [text]="true" (onClick)="remove(item)"/></div></td></tr>}</tbody></table>}@else{<div class="empty-panel"><i class="pi pi-tags"></i><p>لا توجد أنواع مسجلة حتى الآن.</p></div>}</div></section>
  <p-dialog [visible]="show()" (visibleChange)="show.set($event)" [modal]="true" [draggable]="false" [style]="{width:'min(500px, 94vw)'}" [header]="editing() ? 'تعديل النوع' : 'إضافة نوع'"><div class="form-grid"><div class="field span-2"><label>الاسم</label><input pInputText [ngModel]="form().name" (ngModelChange)="form.update(x=>({...x,name:$event}))"/></div>@if(expense){<div class="field span-2"><label>النطاق</label><p-select [options]="scopes" optionLabel="1" optionValue="0" [ngModel]="form().scope" (ngModelChange)="form.update(x=>({...x,scope:+$event}))"/></div>}</div><ng-template pTemplate="footer"><div class="dialog-actions"><p-button label="إلغاء" severity="secondary" [text]="true" (onClick)="show.set(false)"/><p-button label="حفظ" icon="pi pi-check" [loading]="saving()" [disabled]="!form().name.trim()" (onClick)="save()"/></div></ng-template></p-dialog>` })
export class CatalogTypesComponent {
  private readonly http = inject(HttpClient); private readonly base = inject(API_BASE);
  readonly expense = inject(ActivatedRoute).snapshot.data['kind'] === 'expenses'; readonly items = signal<CatalogType[]>([]); readonly show = signal(false); readonly saving = signal(false); readonly editing = signal<CatalogType | null>(null); readonly form = signal({ name: '', scope: ExpenseScope.General, isActive: true }); readonly scopes = optionsFrom(expenseScopeLabels);
  private get url(): string { return `${this.base}/api/${this.expense ? 'expenses' : 'revenues'}/types`; }
  constructor() { this.load(); }
  load(): void { this.http.get<CatalogType[]>(this.url).subscribe({ next: value => this.items.set(value), error: () => undefined }); }
  scopeLabel(item: CatalogType): string { return expenseScopeLabels[(item as ExpenseTypeResponse).scope] ?? ''; }
  open(): void { this.editing.set(null); this.form.set({ name: '', scope: ExpenseScope.General, isActive: true }); this.show.set(true); }
  edit(item: CatalogType): void { this.editing.set(item); this.form.set({ name: item.name, scope: (item as ExpenseTypeResponse).scope ?? ExpenseScope.General, isActive: item.isActive }); this.show.set(true); }
  save(): void { this.saving.set(true); const item = this.editing(); const request = item ? this.http.put(`${this.url}/${item.id}`, this.form()) : this.http.post(this.url, this.form()); request.subscribe({ next: () => { this.show.set(false); this.load(); }, error: () => this.saving.set(false), complete: () => this.saving.set(false) }); }
  remove(item: CatalogType): void { if (confirm('حذف هذا النوع؟')) this.http.delete(`${this.url}/${item.id}`).subscribe({ next: () => this.load(), error: () => undefined }); }
}
