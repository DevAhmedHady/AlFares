import { Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';
import { ChartSeries, ChartType } from '../../core/models';

const DEFAULT_COLORS = ['#2563EB', '#16A34A', '#DC2626', '#D97706', '#7C3AED', '#0891B2', '#DB2777', '#65A30D'];

/** RTL-aware chart built on PrimeNG p-chart (chart.js): bar / pie / line from a ChartSeries. */
@Component({
  selector: 'app-chart',
  standalone: true,
  imports: [ChartModule],
  template: `<p-chart [type]="kind()" [data]="data()" [options]="options()" [style]="{ height: height() }" />`,
})
export class ChartComponent {
  readonly type = input<ChartType>(ChartType.Bar);
  readonly series = input<ChartSeries | null>(null);
  readonly colors = input<string[]>(DEFAULT_COLORS);
  readonly height = input('320px');

  readonly kind = computed(() => (this.type() === ChartType.Pie ? 'pie' : this.type() === ChartType.Line ? 'line' : 'bar'));

  private palette = computed(() => (this.colors()?.length ? this.colors() : DEFAULT_COLORS));

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
          label: s.name, data: values, fill: true, tension: 0.4,
          borderColor: colors[0], backgroundColor: colors[0] + '22', pointBackgroundColor: colors[0],
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
