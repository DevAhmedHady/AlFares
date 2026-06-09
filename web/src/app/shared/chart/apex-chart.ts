import {
  AfterViewInit, Component, ElementRef, OnDestroy, effect, input, viewChild,
} from '@angular/core';
import ApexCharts from 'apexcharts';
import { ChartSeries, ChartType } from '../../core/models';

const DEFAULT_COLORS = ['#2563EB', '#16A34A', '#DC2626', '#D97706', '#7C3AED', '#0891B2', '#DB2777', '#65A30D'];

/** Thin RTL-aware wrapper over ApexCharts rendering bar / pie / line from a ChartSeries. */
@Component({
  selector: 'app-apex-chart',
  standalone: true,
  template: '<div #host class="chart-host"></div>',
  styles: ['.chart-host { width: 100%; min-height: 300px; }'],
})
export class ApexChartComponent implements AfterViewInit, OnDestroy {
  readonly type = input<ChartType>(ChartType.Bar);
  readonly series = input<ChartSeries | null>(null);
  readonly colors = input<string[]>(DEFAULT_COLORS);

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');
  private chart: ApexCharts | null = null;
  private ready = false;

  constructor() {
    effect(() => { this.series(); this.type(); this.colors(); if (this.ready) this.render(); });
  }

  ngAfterViewInit(): void { this.ready = true; this.render(); }
  ngOnDestroy(): void { this.chart?.destroy(); }

  private render(): void {
    const series = this.series();
    if (!series) return;
    this.chart?.destroy();
    this.chart = new ApexCharts(this.host().nativeElement, this.buildOptions(series));
    void this.chart.render();
  }

  private buildOptions(series: ChartSeries): ApexCharts.ApexOptions {
    const labels = series.points.map((p) => p.label);
    const values = series.points.map((p) => p.value);
    const colors = this.colors()?.length ? this.colors() : DEFAULT_COLORS;
    const common = {
      colors,
      chart: { fontFamily: 'Cairo, sans-serif', toolbar: { show: false } },
      legend: { position: 'bottom' as const },
      noData: { text: 'لا توجد بيانات' },
    };

    switch (this.type()) {
      case ChartType.Pie:
        return { ...common, chart: { ...common.chart, type: 'pie' }, labels, series: values };
      case ChartType.Line:
        return {
          ...common,
          chart: { ...common.chart, type: 'line' },
          stroke: { curve: 'smooth', width: 3 },
          xaxis: { categories: labels },
          series: [{ name: series.name, data: values }],
        };
      default:
        return {
          ...common,
          chart: { ...common.chart, type: 'bar' },
          plotOptions: { bar: { borderRadius: 4, distributed: true } },
          xaxis: { categories: labels },
          dataLabels: { enabled: false },
          series: [{ name: series.name, data: values }],
        };
    }
  }
}
