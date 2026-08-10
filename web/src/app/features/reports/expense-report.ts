import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { downloadBlob } from '../../core/api/grid-client';
import { ExpensesService } from '../../core/api/resources';
import { AuthStore } from '../../core/auth/auth.store';
import { monthOptions, validateYmd, yearOptions } from '../../core/date-scope';
import { ExportFormat } from '../../core/grid.models';
import { formatDate, formatMoney, toDate, toIso } from '../../core/labels';
import { ChartSeries, ChartType, ExpenseReportBreakdown, ExpenseReportResponse, ExpenseTypeResponse } from '../../core/models';
import { ChartComponent } from '../../shared/chart/chart';

interface CompositionSegment {
  label: string;
  share: number;
  color: string;
}

@Component({
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    DatePickerModule,
    SelectModule,
    TooltipModule,
    ChartComponent,
  ],
  templateUrl: './expense-report.html',
  styleUrl: './expense-report.scss',
})
export class ExpenseReportComponent {
  private readonly expenses = inject(ExpensesService);
  private readonly store = inject(AuthStore);

  readonly canExport = this.store.has('expenses.export');
  readonly ChartType = ChartType;
  readonly chartColors = ['#c2410c', '#1e40af', '#0d9488', '#7c3aed', '#b45309', '#15803d', '#dc2626', '#0891b2'];

  readonly from = signal('');
  readonly to = signal('');
  readonly forYear = signal<number | null>(null);
  readonly forMonth = signal<number | null>(null);
  readonly typeFilter = signal('');
  readonly types = signal<ExpenseTypeResponse[]>([]);
  readonly data = signal<ExpenseReportResponse | null>(null);
  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly filterError = signal<string | null>(null);
  readonly exporting = signal<ExportFormat | null>(null);
  readonly reveal = signal(false);

  readonly yearOptions = yearOptions();
  readonly monthOptions = monthOptions;

  readonly money = formatMoney;
  readonly date = formatDate;
  readonly toIso = toIso;
  readonly fromModel = computed(() => toDate(this.from()));
  readonly toModel = computed(() => toDate(this.to()));

  readonly usingMonthScope = computed(() => this.forYear() != null && this.forMonth() != null);

  readonly periodLabel = computed(() => {
    const year = this.forYear();
    const month = this.forMonth();
    if (year != null && month != null) {
      const name = this.monthOptions.find((option) => option.value === month)?.label ?? String(month);
      return `${name} ${year}`;
    }
    const from = this.from();
    const to = this.to();
    if (from && to) return `${formatDate(from)} — ${formatDate(to)}`;
    if (from) return `من ${formatDate(from)}`;
    if (to) return `حتى ${formatDate(to)}`;
    return 'كل الفترات';
  });

  readonly categorySeries = computed<ChartSeries | null>(() => {
    const report = this.data();
    if (!report?.byCategory.length) return null;
    return this.toSeries('المصروفات حسب النوع', report.byCategory);
  });

  readonly monthSeries = computed<ChartSeries | null>(() => {
    const report = this.data();
    if (!report?.byMonth.length) return null;
    return this.toSeries('المصروفات عبر الزمن', report.byMonth);
  });

  readonly composition = computed<CompositionSegment[]>(() => {
    const report = this.data();
    if (!report?.byCategory.length) return [];
    return report.byCategory.map((row, index) => ({
      label: row.label,
      share: row.share,
      color: this.chartColors[index % this.chartColors.length],
    }));
  });

  constructor() {
    this.expenses.types().subscribe({
      next: (types) => this.types.set(types),
      error: () => this.types.set([]),
    });
  }

  setFrom(value: Date | null): void {
    this.forYear.set(null);
    this.forMonth.set(null);
    this.from.set(toIso(value));
  }

  setTo(value: Date | null): void {
    this.forYear.set(null);
    this.forMonth.set(null);
    this.to.set(toIso(value));
  }

  setForYear(value: number | null): void {
    this.from.set('');
    this.to.set('');
    this.forYear.set(value);
    if (value == null) this.forMonth.set(null);
  }

  setForMonth(value: number | null): void {
    this.from.set('');
    this.to.set('');
    this.forMonth.set(value);
  }

  load(): void {
    const year = this.forYear();
    const month = this.forMonth();
    const hasMonthScope = year != null || month != null;

    if (hasMonthScope) {
      const ymdError = validateYmd(year, month, null);
      if (ymdError) {
        this.filterError.set(ymdError);
        return;
      }
      if (year == null || month == null) {
        this.filterError.set('اختر السنة والشهر لعرض تقرير شهر كامل.');
        return;
      }
    } else if (this.from() && this.to() && this.from()! > this.to()!) {
      this.filterError.set('يجب أن يكون تاريخ البداية قبل تاريخ النهاية.');
      return;
    }

    this.filterError.set(null);
    this.loading.set(true);
    this.reveal.set(false);
    this.expenses.report(this.requestBody()).subscribe({
      next: (report) => {
        this.data.set(report);
        this.loaded.set(true);
        this.loading.set(false);
        requestAnimationFrame(() => this.reveal.set(true));
      },
      error: () => {
        this.data.set(null);
        this.loaded.set(true);
        this.loading.set(false);
      },
    });
  }

  exportAs(format: ExportFormat): void {
    if (!this.canExport) return;
    this.exporting.set(format);
    this.expenses.reportExport(format, this.requestBody()).subscribe({
      next: (blob) => {
        const ext = format === ExportFormat.Xlsx ? 'xlsx' : 'pdf';
        downloadBlob(blob, `expense-report.${ext}`);
        this.exporting.set(null);
      },
      error: () => this.exporting.set(null),
    });
  }

  readonly Xlsx = ExportFormat.Xlsx;
  readonly Pdf = ExportFormat.Pdf;

  private requestBody() {
    if (this.usingMonthScope()) {
      return {
        from: null,
        to: null,
        year: this.forYear(),
        month: this.forMonth(),
        expenseTypeId: this.typeFilter() || null,
      };
    }
    return {
      from: this.from() || null,
      to: this.to() || null,
      year: null,
      month: null,
      expenseTypeId: this.typeFilter() || null,
    };
  }

  private toSeries(name: string, rows: ExpenseReportBreakdown[]): ChartSeries {
    return { name, points: rows.map((row) => ({ label: row.label, value: row.amount })) };
  }
}
