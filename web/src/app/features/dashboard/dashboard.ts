import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { ChartComponent } from '../../shared/chart/chart';
import { ChartBuilderComponent } from '../chart-builder/chart-builder';
import { DashboardService } from '../../core/api/dashboard.service';
import { AuthStore } from '../../core/auth/auth.store';
import { ChartDefinition, ChartSeries } from '../../core/models';
import { chartTypeLabels } from '../../core/labels';
import { MessageService } from 'primeng/api';
import { forkJoin } from 'rxjs';

type ChartLoadState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule, DialogModule, TooltipModule, ChartComponent, ChartBuilderComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent {
  private readonly service = inject(DashboardService);
  private readonly store = inject(AuthStore);
  private readonly messages = inject(MessageService);

  readonly charts = signal<ChartDefinition[]>([]);
  readonly data = signal<Record<string, ChartSeries>>({});
  readonly loading = signal(true);
  readonly listError = signal(false);
  readonly chartStates = signal<Record<string, ChartLoadState>>({});
  readonly loadedAt = signal<Date | null>(null);
  readonly builderOpen = signal(false);
  readonly editing = signal<ChartDefinition | null>(null);
  readonly managingOrder = signal(false);
  readonly orderDirty = signal(false);
  readonly savingOrder = signal(false);

  readonly canManage = this.store.has('dashboard.charts.manage');
  readonly typeLabels = chartTypeLabels;
  readonly readyCharts = computed(() => Object.values(this.chartStates()).filter((state) => state === 'ready').length);
  readonly totalPoints = computed(() => Object.values(this.data()).reduce((total, series) => total + series.points.length, 0));
  readonly loadedAtLabel = computed(() => {
    const value = this.loadedAt();
    return value ? new Intl.DateTimeFormat('ar-EG', { hour: '2-digit', minute: '2-digit' }).format(value) : '—';
  });
  private readonly chartColors = computed(() => {
    const map: Record<string, string[]> = {};
    for (const chart of this.charts()) {
      try { map[chart.id] = JSON.parse(chart.colorsJson) as string[]; } catch { map[chart.id] = []; }
    }
    return map;
  });
  private dragId: string | null = null;
  private orderSnapshot: ChartDefinition[] = [];

  constructor() { this.load(); }

  load(): void {
    this.loading.set(true);
    this.listError.set(false);
    this.data.set({});
    this.chartStates.set({});
    this.service.charts().subscribe({
      next: (charts) => {
        const sorted = [...charts].sort((a, b) => a.layoutOrder - b.layoutOrder);
        this.charts.set(sorted);
        this.chartStates.set(Object.fromEntries(sorted.map((chart) => [chart.id, 'loading'])));
        this.loading.set(false);
        this.loadedAt.set(new Date());
        for (const chart of sorted) this.loadChart(chart.id);
      },
      error: () => {
        this.loading.set(false);
        this.listError.set(true);
      },
    });
  }

  loadChart(id: string): void {
    this.chartStates.update((states) => ({ ...states, [id]: 'loading' }));
    this.service.data(id).subscribe({
      next: (series) => {
        this.data.update((data) => ({ ...data, [id]: series }));
        this.chartStates.update((states) => ({ ...states, [id]: 'ready' }));
        this.loadedAt.set(new Date());
      },
      error: () => this.chartStates.update((states) => ({ ...states, [id]: 'error' })),
    });
  }

  colors(chart: ChartDefinition): string[] {
    return this.chartColors()[chart.id] ?? [];
  }

  openCreate(): void { this.editing.set(null); this.builderOpen.set(true); }
  openEdit(chart: ChartDefinition): void { this.editing.set(chart); this.builderOpen.set(true); }

  onSaved(): void {
    this.builderOpen.set(false);
    this.messages.add({ severity: 'success', summary: 'تم الحفظ', detail: 'تم تحديث الرسم بنجاح' });
    this.load();
  }

  remove(chart: ChartDefinition): void {
    if (!confirm(`حذف الرسم "${chart.title}"؟`)) return;
    this.service.remove(chart.id).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: 'تم الحذف', detail: `تم حذف الرسم ${chart.title}` });
        this.load();
      },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر الحذف', detail: 'أعد المحاولة بعد قليل' }),
    });
  }

  beginOrdering(): void {
    this.orderSnapshot = [...this.charts()];
    this.managingOrder.set(true);
    this.orderDirty.set(false);
  }

  cancelOrdering(): void {
    this.charts.set([...this.orderSnapshot]);
    this.managingOrder.set(false);
    this.orderDirty.set(false);
    this.dragId = null;
  }

  onDragStart(id: string): void {
    if (this.managingOrder()) this.dragId = id;
  }

  onDrop(targetId: string): void {
    if (!this.managingOrder() || !this.dragId || this.dragId === targetId) return;
    const list = [...this.charts()];
    const from = list.findIndex((c) => c.id === this.dragId);
    const to = list.findIndex((c) => c.id === targetId);
    this.reorder(from, to);
    this.dragId = null;
  }

  moveChart(index: number, delta: number): void {
    const target = index + delta;
    if (!this.managingOrder() || target < 0 || target >= this.charts().length) return;
    this.reorder(index, target);
  }

  saveOrdering(): void {
    const changed = this.charts()
      .map((chart, index) => ({ chart, index }))
      .filter(({ chart, index }) => chart.layoutOrder !== index);

    if (changed.length === 0) {
      this.managingOrder.set(false);
      this.orderDirty.set(false);
      return;
    }

    this.savingOrder.set(true);
    forkJoin(changed.map(({ chart, index }) => this.service.update(chart.id, { ...toRequest(chart), layoutOrder: index })))
      .subscribe({
        next: () => {
          this.charts.update((charts) => charts.map((chart, index) => ({ ...chart, layoutOrder: index })));
          this.savingOrder.set(false);
          this.managingOrder.set(false);
          this.orderDirty.set(false);
          this.messages.add({ severity: 'success', summary: 'تم الحفظ', detail: 'تم حفظ ترتيب الرسوم' });
        },
        error: () => {
          this.savingOrder.set(false);
          this.messages.add({ severity: 'error', summary: 'تعذر حفظ الترتيب', detail: 'تمت استعادة الترتيب المحفوظ' });
          this.cancelOrdering();
          this.load();
        },
      });
  }

  private reorder(from: number, to: number): void {
    if (from < 0 || to < 0 || from === to) return;
    this.charts.set(moveItem(this.charts(), from, to));
    this.orderDirty.set(true);
  }
}

/** Returns a copy with one item moved to a new position. */
export function moveItem<T>(items: readonly T[], from: number, to: number): T[] {
  const result = [...items];
  if (from < 0 || to < 0 || from >= result.length || to >= result.length || from === to) return result;
  const [moved] = result.splice(from, 1);
  result.splice(to, 0, moved);
  return result;
}

function toRequest(c: ChartDefinition) {
  return {
    title: c.title, type: c.type, datasourceKey: c.datasourceKey, xField: c.xField,
    yField: c.yField, aggregation: c.aggregation, colorsJson: c.colorsJson,
    filtersJson: c.filtersJson, layoutOrder: c.layoutOrder,
  };
}
