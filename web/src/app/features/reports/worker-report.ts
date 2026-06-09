import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { WorkersService } from '../../core/api/resources';
import { formatMoney } from '../../core/labels';
import { WorkerReportResponse } from '../../core/models';

@Component({ standalone: true, imports: [FormsModule, ButtonModule, InputTextModule], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-id-card"></i></span><div><h1>تقرير العامل</h1><p>كشف السلف والتسويات والرصيد المتحرك للعامل.</p></div></div></header>
  <div class="report-panel"><div class="report-filters"><div class="field"><label>معرف العامل</label><input pInputText [ngModel]="id()" (ngModelChange)="id.set($event)" placeholder="ألصق معرف العامل"/></div><div class="field"><label>من تاريخ</label><input pInputText type="date" [ngModel]="from()" (ngModelChange)="from.set($event)"/></div><div class="field"><label>إلى تاريخ</label><input pInputText type="date" [ngModel]="to()" (ngModelChange)="to.set($event)"/></div><p-button label="عرض التقرير" icon="pi pi-search" [loading]="loading()" [disabled]="!id()" (onClick)="load()"/></div></div>
  @if(data()){<div class="summary-grid"><div class="summary-card negative"><span>إجمالي السلف</span><strong>{{money(data()!.totalAdvances)}}</strong></div><div class="summary-card positive"><span>إجمالي التسويات</span><strong>{{money(data()!.totalSettlements)}}</strong></div><div class="summary-card"><span>الرصيد الحالي</span><strong>{{money(data()!.balance)}}</strong></div></div><div class="report-panel">@if(data()!.transactions.length){<table class="data-table"><thead><tr><th>التاريخ</th><th>الحركة</th><th>المبلغ</th><th>الرصيد</th></tr></thead><tbody>@for(item of data()!.transactions;track item.id){<tr><td>{{item.date}}</td><td>{{item.kind}}</td><td>{{money(item.amount)}}</td><td>{{money(item.runningBalance)}}</td></tr>}</tbody></table>}@else{<div class="empty-panel">لا توجد حركات ضمن الفترة المحددة.</div>}</div>}</section>` })
export class WorkerReportComponent {
  private readonly workers = inject(WorkersService); readonly id = signal(''); readonly from = signal(''); readonly to = signal(''); readonly data = signal<WorkerReportResponse | null>(null); readonly loading = signal(false); readonly money = formatMoney;
  load(): void { if (!this.id()) return; this.loading.set(true); this.workers.report({ workerId: this.id(), from: this.from() || null, to: this.to() || null }).subscribe({ next: value => this.data.set(value), error: () => this.loading.set(false), complete: () => this.loading.set(false) }); }
}
