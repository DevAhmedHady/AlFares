import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ReportsService } from '../../core/api/resources';
import { formatMoney } from '../../core/labels';
import { OwnerLedgerResponse, OwnerType } from '../../core/models';

@Component({ standalone: true, imports: [FormsModule, ButtonModule, InputTextModule, SelectModule], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-book"></i></span><div><h1>كشف حساب</h1><p>تجميع الإيرادات والمصروفات حسب العميل أو السيارة.</p></div></div></header>
  <div class="report-panel"><div class="report-filters"><div class="field"><label>نوع الحساب</label><p-select [options]="owners" optionLabel="label" optionValue="value" [ngModel]="ownerType()" (ngModelChange)="ownerType.set($event)"/></div><div class="field"><label>معرف العميل أو السيارة</label><input pInputText [ngModel]="ownerId()" (ngModelChange)="ownerId.set($event)" placeholder="ألصق المعرف هنا"/></div><div class="field"><label>من تاريخ</label><input pInputText type="date" [ngModel]="from()" (ngModelChange)="from.set($event)"/></div><div class="field"><label>إلى تاريخ</label><input pInputText type="date" [ngModel]="to()" (ngModelChange)="to.set($event)"/></div></div><div style="margin-top:.8rem"><p-button label="عرض التقرير" icon="pi pi-search" [loading]="loading()" [disabled]="!ownerId()" (onClick)="load()"/></div></div>
  @if(data()){<div class="summary-grid"><div class="summary-card positive"><span>إجمالي الإيرادات</span><strong>{{money(data()!.totalRevenues)}}</strong></div><div class="summary-card negative"><span>إجمالي المصروفات</span><strong>{{money(data()!.totalExpenses)}}</strong></div><div class="summary-card" [class.positive]="data()!.net>=0" [class.negative]="data()!.net<0"><span>صافي الحساب</span><strong>{{money(data()!.net)}}</strong></div></div><div class="report-panel">@if(data()!.entries.length){<table class="data-table"><thead><tr><th>التاريخ</th><th>البيان</th><th>المبلغ</th></tr></thead><tbody>@for(item of data()!.entries;track item.id){<tr><td>{{item.date}}</td><td>{{item.description}}</td><td>{{money(item.amount)}}</td></tr>}</tbody></table>}@else{<div class="empty-panel">لا توجد حركات ضمن الفترة المحددة.</div>}</div>}</section>` })
export class OwnerReportComponent {
  private readonly reports = inject(ReportsService); readonly ownerType = signal(OwnerType.Client); readonly ownerId = signal(''); readonly from = signal(''); readonly to = signal(''); readonly data = signal<OwnerLedgerResponse | null>(null); readonly loading = signal(false); readonly money = formatMoney;
  readonly owners = [{ label: 'عميل', value: OwnerType.Client }, { label: 'سيارة مملوكة', value: OwnerType.OwnedCar }, { label: 'سيارة مؤجرة', value: OwnerType.RentedCar }];
  load(): void { if (!this.ownerId()) return; this.loading.set(true); this.reports.ledger(this.ownerType(), this.ownerId(), this.from(), this.to()).subscribe({ next: value => this.data.set(value), error: () => this.loading.set(false), complete: () => this.loading.set(false) }); }
}
