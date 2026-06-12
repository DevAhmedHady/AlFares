import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';
import { ChartSeries, ChartType } from '../../core/models';

const DEFAULT_COLORS = ['#2563EB', '#16A34A', '#DC2626', '#D97706', '#7C3AED', '#0891B2', '#DB2777', '#65A30D'];

/** RTL-aware chart built on PrimeNG p-chart (chart.js): bar / pie / line from a ChartSeries. */
@Component({
  selector: 'app-chart',
  standalone: true,
  imports: [ChartModule, DecimalPipe],
  template: `
    <figure class="chart-figure" [attr.aria-label]="accessibleLabel()">
      <p class="chart-summary">{{ insight() }}</p>
      <div role="img" [attr.aria-label]="accessibleLabel()">
        <p-chart [type]="kind()" [data]="data()" [options]="options()" [style]="{ height: height() }" />
      </div>
      <details class="chart-data">
        <summary>عرض البيانات</summary>
        <div class="table-wrap">
          <table>
            <caption>{{ title() }}</caption>
            <thead><tr><th scope="col">التصنيف</th><th scope="col">القيمة</th></tr></thead>
            <tbody>
              @for (point of series()?.points ?? []; track point.label) {
                <tr><td>{{ point.label }}</td><td>{{ point.value | number:'1.0-2' }}</td></tr>
              }
            </tbody>
          </table>
        </div>
      </details>
    </figure>
  `,
  styles: [`
    .chart-figure { margin: 0; }
    .chart-summary { margin: 0 0 .5rem; color: #475569; font-size: .85rem; line-height: 1.7; }
    .chart-data { margin-top: .5rem; border-top: 1px solid #e2e8f0; padding-top: .6rem; }
    .chart-data summary { width: fit-content; color: #1d4ed8; cursor: pointer; font-size: .82rem; font-weight: 600; }
    .chart-data summary:focus-visible { outline: 3px solid #bfdbfe; outline-offset: 3px; border-radius: 4px; }
    .table-wrap { overflow-x: auto; margin-top: .6rem; }
    table { width: 100%; border-collapse: collapse; font-size: .82rem; }
    caption { text-align: right; font-weight: 700; margin-bottom: .4rem; }
    th, td { padding: .5rem; border-bottom: 1px solid #e2e8f0; text-align: right; }
    th:last-child, td:last-child { text-align: left; font-variant-numeric: tabular-nums; }
  `],
})
export class ChartComponent {
  readonly title = input('رسم بياني');
  readonly type = input<ChartType>(ChartType.Bar);
  readonly series = input<ChartSeries | null>(null);
  readonly colors = input<string[]>(DEFAULT_COLORS);
  readonly height = input('320px');

  readonly kind = computed(() => (this.type() === ChartType.Pie ? 'pie' : this.type() === ChartType.Line ? 'line' : 'bar'));

  private palette = computed(() => (this.colors()?.length ? this.colors() : DEFAULT_COLORS));
  readonly accessibleLabel = computed(() => `${this.title()}. ${this.insight()}`);
  readonly insight = computed(() => buildChartInsight(this.series()));

  readonly data = computed(() => {
    const s = this.series();
    if (!s) return { labels: [], datasets: [] };
    const labels = s.points.map((p) => p.label);
    const values = s.points.map((p) => p.value);
    const colors = this.palette();
    const type = this.type();

    if (type === ChartType.Pie) {
      return { labels, datasets: [{ data: values, backgroundColor: colors, borderWidth: 0 }] };
    }
    if (type === ChartType.Line) {
      return {
        labels,
        datasets: [{
          label: s.name, data: values, fill: false, tension: 0.2,
          borderColor: colors[0], backgroundColor: colors[0], pointBackgroundColor: colors[0],
        }],
      };
    }
    return {
      labels,
      datasets: [{ label: s.name, data: values, backgroundColor: labels.map((_, i) => colors[i % colors.length]), borderRadius: 6 }],
    };
  });

  readonly options = computed(() => {
    const isPie = this.type() === ChartType.Pie;
    const text = '#334155';
    const grid = '#e2e8f0';
    return {
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'bottom', labels: { color: text, font: { family: 'Cairo' }, usePointStyle: true } },
        tooltip: { rtl: true, bodyFont: { family: 'Cairo' }, titleFont: { family: 'Cairo' } },
      },
      scales: isPie ? {} : {
        x: { ticks: { color: text, font: { family: 'Cairo' } }, grid: { color: grid }, reverse: true },
        y: { ticks: { color: text, font: { family: 'Cairo' } }, grid: { color: grid }, beginAtZero: true },
      },
    };
  });
}

/** Builds a concise Arabic textual equivalent for a chart series. */
export function buildChartInsight(series: ChartSeries | null): string {
  const points = series?.points ?? [];
  if (points.length === 0) return 'لا توجد بيانات متاحة لهذا المؤشر.';
  const top = points.reduce((current, point) => point.value > current.value ? point : current, points[0]);
  return `يعرض المؤشر ${points.length} تصنيفات. أعلى قيمة: ${top.label} (${new Intl.NumberFormat('ar-EG', { maximumFractionDigits: 2 }).format(top.value)}).`;
}
