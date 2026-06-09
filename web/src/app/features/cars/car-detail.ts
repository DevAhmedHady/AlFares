import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CarsService, ReportsService } from '../../core/api/resources';
import { carTypeLabels, formatMoney } from '../../core/labels';
import { CarResponse, OwnerLedgerResponse, OwnerType } from '../../core/models';

@Component({ standalone: true, imports: [RouterLink, ButtonModule], template: `
  <section class="feature-page"><header class="feature-hero"><div class="feature-title"><span class="feature-icon"><i class="pi pi-car"></i></span><div><h1>{{car()?.name || 'تفاصيل السيارة'}}</h1><p>@if(car()){ {{carTypeLabels[car()!.type]}} · {{car()!.plateNumber || 'بدون لوحة'}} · {{car()!.driverName || 'بدون سائق'}} }</p></div></div><p-button label="العودة" icon="pi pi-arrow-right" severity="secondary" [text]="true" routerLink="/cars"/></header>
  @if(summary()){<div class="summary-grid"><div class="summary-card negative"><span>إجمالي المصروفات</span><strong>{{money(summary()!.totalExpenses)}}</strong></div><div class="summary-card positive"><span>إجمالي الإيرادات</span><strong>{{money(summary()!.totalRevenues)}}</strong></div><div class="summary-card" [class.positive]="summary()!.net>=0" [class.negative]="summary()!.net<0"><span>الصافي</span><strong>{{money(summary()!.net)}}</strong></div></div><div class="report-panel">@if(summary()!.entries.length){<table class="data-table"><thead><tr><th>التاريخ</th><th>البيان</th><th>المبلغ</th></tr></thead><tbody>@for(item of summary()!.entries;track item.id){<tr><td>{{item.date}}</td><td>{{item.description}}</td><td>{{money(item.amount)}}</td></tr>}</tbody></table>}@else{<div class="empty-panel">لا توجد حركات مالية لهذه السيارة.</div>}</div>} @else {<div class="report-panel empty-panel"><i class="pi pi-spin pi-spinner"></i><p>جار تحميل كشف السيارة...</p></div>}</section>` })
export class CarDetailComponent {
  private readonly cars = inject(CarsService); private readonly reports = inject(ReportsService);
  readonly car = signal<CarResponse | null>(null); readonly summary = signal<OwnerLedgerResponse | null>(null); readonly money = formatMoney; readonly carTypeLabels = carTypeLabels;
  constructor() { const id = inject(ActivatedRoute).snapshot.paramMap.get('id')!; this.cars.getById(id).subscribe({ next: car => { this.car.set(car); this.reports.ledger(car.type === 0 ? OwnerType.OwnedCar : OwnerType.RentedCar, id).subscribe({ next: value => this.summary.set(value), error: () => undefined }); }, error: () => undefined }); }
}
