import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { WorkersService } from '../../core/api/resources';
import { emptyGridQuery } from '../../core/grid.models';
import { formatDate, formatMoney, toDate, toIso } from '../../core/labels';
import { WorkerReportResponse } from '../../core/models';

interface WorkerOption { id: string; label: string; }

@Component({ standalone: true, imports: [FormsModule, ButtonModule, DatePickerModule, SelectModule, TagModule], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-id-card"></i></span><div><h1>تقرير العامل</h1><p>كشف السلف والتسويات والرصيد المتحرك للعامل.</p></div></div></header>
  <div class="report-panel"><div class="report-filters"><div class="field"><label>العامل</label><p-select [options]="workers()" optionLabel="label" optionValue="id" [filter]="true" filterBy="label" [ngModel]="id()" (ngModelChange)="id.set($event)" [loading]="loadingWorkers()" placeholder="اختر العامل" emptyMessage="لا يوجد عمال"/></div><div class="field"><label>من تاريخ</label><p-datepicker [ngModel]="fromModel()" (ngModelChange)="from.set(toIso($event))" dateFormat="dd/mm/yy" [showIcon]="true" iconDisplay="input" [showButtonBar]="true" appendTo="body" styleClass="w-full" inputStyleClass="w-full"/></div><div class="field"><label>إلى تاريخ</label><p-datepicker [ngModel]="toModel()" (ngModelChange)="to.set(toIso($event))" dateFormat="dd/mm/yy" [showIcon]="true" iconDisplay="input" [showButtonBar]="true" appendTo="body" styleClass="w-full" inputStyleClass="w-full"/></div><div class="field" style="display:flex;align-items:flex-end"><p-button label="عرض التقرير" icon="pi pi-search" [loading]="loading()" [disabled]="!id()" (onClick)="load()"/></div></div></div>
  @if(data(); as report){<div class="summary-grid"><div class="summary-card negative"><span>إجمالي السلف</span><strong>{{money(report.totalAdvances)}}</strong></div><div class="summary-card positive"><span>إجمالي التسويات</span><strong>{{money(report.totalSettlements)}}</strong></div><div class="summary-card" [class.negative]="report.balance>0"><span>الرصيد الحالي</span><strong>{{money(report.balance)}}</strong></div></div><div class="report-panel">@if(report.transactions.length){<table class="data-table"><thead><tr><th>التاريخ</th><th>الحركة</th><th class="num">المبلغ</th><th class="num">الرصيد</th></tr></thead><tbody>@for(item of report.transactions;track item.id){<tr><td>{{date(item.date)}}</td><td>@if(item.kind==='تسوية'){<p-tag severity="success" [value]="item.kind"/>}@else{<p-tag severity="warn" [value]="item.kind"/>}</td><td class="num" [class.amount-in]="item.kind==='تسوية'" [class.amount-out]="item.kind!=='تسوية'">{{item.kind==='تسوية'?'−':'+'}} {{money(item.amount)}}</td><td class="num">{{money(item.runningBalance)}}</td></tr>}</tbody></table>}@else{<div class="empty-panel"><i class="pi pi-inbox"></i><p>لا توجد حركات ضمن الفترة المحددة.</p></div>}</div>}@else if(loaded()){<div class="report-panel empty-panel"><i class="pi pi-search"></i><p>اختر عاملًا ثم اعرض التقرير.</p></div>}</section>` })
export class WorkerReportComponent {
  private readonly workersApi = inject(WorkersService);
  readonly id = signal('');
  readonly from = signal('');
  readonly to = signal('');
  readonly data = signal<WorkerReportResponse | null>(null);
  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly workers = signal<WorkerOption[]>([]);
  readonly loadingWorkers = signal(false);
  readonly money = formatMoney;
  readonly date = formatDate;
  readonly toIso = toIso;
  readonly fromModel = computed(() => toDate(this.from()));
  readonly toModel = computed(() => toDate(this.to()));

  constructor() {
    this.loadingWorkers.set(true);
    this.workersApi.grid(emptyGridQuery(500)).subscribe({
      next: page => this.workers.set(page.items.map(w => ({ id: w.id, label: w.jobTitle ? `${w.name} · ${w.jobTitle}` : w.name }))),
      error: () => this.workers.set([]),
      complete: () => this.loadingWorkers.set(false),
    });
  }

  load(): void {
    if (!this.id()) return;
    this.loading.set(true);
    this.loaded.set(true);
    this.workersApi.report({ workerId: this.id(), from: this.from() || null, to: this.to() || null }).subscribe({
      next: value => this.data.set(value),
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
  }
}
