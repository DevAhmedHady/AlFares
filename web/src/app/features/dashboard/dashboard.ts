import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { ChartComponent } from '../../shared/chart/chart';
import { ChartBuilderComponent } from '../chart-builder/chart-builder';
import { DashboardService } from '../../core/api/dashboard.service';
import { AuthStore } from '../../core/auth/auth.store';
import { ChartDefinition, ChartSeries } from '../../core/models';
import { chartTypeLabels } from '../../core/labels';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule, DialogModule, TagModule, ChartComponent, ChartBuilderComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent {
  private readonly service = inject(DashboardService);
  private readonly store = inject(AuthStore);

  readonly charts = signal<ChartDefinition[]>([]);
  readonly data = signal<Record<string, ChartSeries>>({});
  readonly loading = signal(true);
  readonly builderOpen = signal(false);
  readonly editing = signal<ChartDefinition | null>(null);

  readonly canManage = this.store.has('dashboard.charts.manage');
  readonly typeLabels = chartTypeLabels;
  private dragId: string | null = null;

  constructor() { this.load(); }

  load(): void {
    this.loading.set(true);
    this.service.charts().subscribe({
      next: (charts) => {
        this.charts.set([...charts].sort((a, b) => a.layoutOrder - b.layoutOrder));
        this.loading.set(false);
        for (const chart of charts) {
          this.service.data(chart.id).subscribe({
            next: (series) => this.data.update((d) => ({ ...d, [chart.id]: series })),
            error: () => this.data.update((d) => {
              const next = { ...d };
              delete next[chart.id];
              return next;
            }),
          });
        }
      },
      error: () => this.loading.set(false),
    });
  }

  colors(chart: ChartDefinition): string[] {
    try { return JSON.parse(chart.colorsJson) as string[]; } catch { return []; }
  }

  openCreate(): void { this.editing.set(null); this.builderOpen.set(true); }
  openEdit(chart: ChartDefinition): void { this.editing.set(chart); this.builderOpen.set(true); }

  onSaved(): void { this.builderOpen.set(false); this.load(); }

  remove(chart: ChartDefinition): void {
    if (!confirm(`حذف الرسم "${chart.title}"؟`)) return;
    this.service.remove(chart.id).subscribe(() => this.load());
  }

  onDragStart(id: string): void { this.dragId = id; }
  onDrop(targetId: string): void {
    if (!this.dragId || this.dragId === targetId) return;
    const list = [...this.charts()];
    const from = list.findIndex((c) => c.id === this.dragId);
    const to = list.findIndex((c) => c.id === targetId);
    const [moved] = list.splice(from, 1);
    list.splice(to, 0, moved);
    this.charts.set(list);
    this.dragId = null;
    // Persist new order.
    list.forEach((chart, index) => {
      if (chart.layoutOrder !== index) {
        this.service.update(chart.id, { ...toRequest(chart), layoutOrder: index }).subscribe();
      }
    });
  }
}

function toRequest(c: ChartDefinition) {
  return {
    title: c.title, type: c.type, datasourceKey: c.datasourceKey, xField: c.xField,
    yField: c.yField, aggregation: c.aggregation, colorsJson: c.colorsJson,
    filtersJson: c.filtersJson, layoutOrder: c.layoutOrder,
  };
}
