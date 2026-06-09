import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApexChartComponent } from '../../shared/chart/apex-chart';
import { DashboardService } from '../../core/api/dashboard.service';
import {
  ChartAggregation, ChartDataSourceMetadata, ChartDefinition, ChartRequest, ChartSeries, ChartType,
} from '../../core/models';
import { chartTypeLabels, optionsFrom } from '../../core/labels';

const PALETTES: Record<string, string[]> = {
  افتراضي: ['#2563EB', '#16A34A', '#DC2626', '#D97706', '#7C3AED', '#0891B2', '#DB2777', '#65A30D'],
  دافئ: ['#DC2626', '#D97706', '#DB2777', '#F59E0B', '#EF4444', '#FB923C'],
  بارد: ['#2563EB', '#0891B2', '#7C3AED', '#0EA5E9', '#14B8A6', '#6366F1'],
};

const AGG_LABELS: Record<number, string> = {
  [ChartAggregation.Count]: 'عدّ', [ChartAggregation.Sum]: 'مجموع',
  [ChartAggregation.Avg]: 'متوسط', [ChartAggregation.Min]: 'أدنى', [ChartAggregation.Max]: 'أقصى',
};

/** Admin chart designer: type → datasource → X/Y + aggregation → palette → live preview → save. */
@Component({
  selector: 'app-chart-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, ApexChartComponent],
  templateUrl: './chart-builder.html',
})
export class ChartBuilderComponent {
  private readonly service = inject(DashboardService);

  readonly existing = input<ChartDefinition | null>(null);
  readonly saved = output<void>();
  readonly cancelled = output<void>();

  readonly datasources = signal<ChartDataSourceMetadata[]>([]);
  readonly preview = signal<ChartSeries | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly title = signal('');
  readonly type = signal<ChartType>(ChartType.Bar);
  readonly datasourceKey = signal('');
  readonly xField = signal('');
  readonly yField = signal('');
  readonly aggregation = signal<ChartAggregation>(ChartAggregation.Count);
  readonly paletteName = signal('افتراضي');

  readonly typeOptions = optionsFrom(chartTypeLabels);
  readonly aggOptions = optionsFrom(AGG_LABELS);
  readonly paletteNames = Object.keys(PALETTES);
  readonly Count = ChartAggregation.Count;

  readonly selectedSource = computed(() => this.datasources().find((d) => d.key === this.datasourceKey()));
  readonly xOptions = computed(() => this.selectedSource()?.fields.filter((f) => f.canGroupBy) ?? []);
  readonly yOptions = computed(() => this.selectedSource()?.fields.filter((f) => f.canAggregate) ?? []);
  readonly palette = computed(() => PALETTES[this.paletteName()] ?? PALETTES['افتراضي']);
  readonly needsY = computed(() => this.aggregation() !== ChartAggregation.Count);

  constructor() {
    this.service.datasources().subscribe({
      next: (ds) => { this.datasources.set(ds); this.hydrate(); },
      error: () => this.error.set('تعذر تحميل مصادر البيانات'),
    });
  }

  private hydrate(): void {
    const e = this.existing();
    if (!e) { this.datasourceKey.set(this.datasources()[0]?.key ?? ''); this.syncDefaults(); return; }
    this.title.set(e.title); this.type.set(e.type); this.datasourceKey.set(e.datasourceKey);
    this.xField.set(e.xField); this.yField.set(e.yField ?? ''); this.aggregation.set(e.aggregation);
    const name = Object.keys(PALETTES).find((k) => JSON.stringify(PALETTES[k]) === e.colorsJson);
    if (name) this.paletteName.set(name);
    this.refresh();
  }

  onDatasourceChange(): void { this.syncDefaults(); }

  private syncDefaults(): void {
    this.xField.set(this.xOptions()[0]?.key ?? '');
    this.yField.set(this.yOptions()[0]?.key ?? '');
    this.refresh();
  }

  private buildRequest(): ChartRequest {
    return {
      title: this.title() || 'رسم بدون عنوان',
      type: this.type(),
      datasourceKey: this.datasourceKey(),
      xField: this.xField(),
      yField: this.needsY() ? (this.yField() || null) : null,
      aggregation: this.aggregation(),
      colorsJson: JSON.stringify(this.palette()),
      filtersJson: this.existing()?.filtersJson ?? null,
      layoutOrder: this.existing()?.layoutOrder ?? 0,
    };
  }

  refresh(): void {
    if (!this.datasourceKey() || !this.xField()) return;
    this.error.set(null);
    this.service.preview(this.buildRequest()).subscribe({
      next: (series) => this.preview.set(series),
      error: (e) => this.error.set(e?.error?.description ?? 'تعذر إنشاء المعاينة'),
    });
  }

  save(): void {
    this.saving.set(true);
    const e = this.existing();
    const req = e ? this.service.update(e.id, this.buildRequest()) : this.service.create(this.buildRequest());
    req.subscribe({
      next: () => { this.saving.set(false); this.saved.emit(); },
      error: (err) => { this.error.set(err?.error?.description ?? 'فشل الحفظ'); this.saving.set(false); },
    });
  }
}
