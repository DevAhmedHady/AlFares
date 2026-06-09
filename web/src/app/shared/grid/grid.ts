import { CommonModule } from '@angular/common';
import {
  Component, TemplateRef, computed, effect, inject, input, output, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '../../core/auth/auth.store';
import { downloadBlob } from '../../core/api/grid-client';
import {
  ExportFormat, GridFieldType, GridFilter, GridFilterOp, GridQuery, GridSort, PagedResult, emptyGridQuery,
} from '../../core/grid.models';
import { ColumnDef, GridSource } from './grid-column';

/* eslint-disable @typescript-eslint/no-explicit-any */
type Row = any;

/** Reusable RTL server-side grid: sort, global + per-column filter, column reorder/show-hide, export. */
@Component({
  selector: 'app-grid',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './grid.html',
  styleUrl: './grid.scss',
})
export class GridComponent {
  private readonly store = inject(AuthStore);

  readonly title = input('');
  readonly columns = input<ColumnDef<any>[]>([]);
  readonly source = input.required<GridSource<any>>();
  readonly pageSize = input(25);
  readonly exportName = input('export');
  readonly exportPermission = input<string | null>(null);
  readonly createPermission = input<string | null>(null);
  readonly rowActions = input<TemplateRef<unknown> | null>(null);
  readonly createClicked = output<void>();

  readonly result = signal<PagedResult<Row> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly search = signal('');
  private readonly sort = signal<GridSort | null>(null);
  private readonly columnFilters = signal<Record<string, string>>({});
  private readonly page = signal(1);

  readonly order = signal<string[]>([]);
  readonly hidden = signal<Set<string>>(new Set());
  readonly showColumnMenu = signal(false);
  private dragKey: string | null = null;

  readonly orderedColumns = computed<ColumnDef[]>(() => {
    const byKey = new Map(this.columns().map((c) => [c.key, c]));
    return this.order()
      .map((k) => byKey.get(k))
      .filter((c): c is ColumnDef => !!c && !this.hidden().has(c.key));
  });

  readonly canExport = computed(() => this.store.has(this.exportPermission()));
  readonly canCreate = computed(() => this.store.has(this.createPermission()));
  readonly Field = GridFieldType;

  constructor() {
    effect(() => {
      const cols = this.columns();
      if (cols.length && this.order().length === 0) {
        this.order.set(cols.map((c) => c.key));
      }
    });
    // Reload whenever the composed query changes.
    effect(() => { this.query(); this.load(); });
  }

  private query = computed<GridQuery>(() => {
    const q = emptyGridQuery(this.pageSize());
    q.page = this.page();
    q.search = this.search().trim() || null;
    const s = this.sort();
    q.sort = s ? [s] : [];
    q.filters = this.buildFilters();
    return q;
  });

  private buildFilters(): GridFilter[] {
    const filters: GridFilter[] = [];
    const map = this.columnFilters();
    for (const col of this.columns()) {
      const raw = map[col.key];
      if (raw === undefined || raw === null || raw === '') continue;
      filters.push({ field: col.key, op: this.opFor(col.type), value: String(raw) });
    }
    return filters;
  }

  private opFor(type: GridFieldType): GridFilterOp {
    return type === GridFieldType.Text ? GridFilterOp.Contains : GridFilterOp.Eq;
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.source().grid(this.query()).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (e) => {
        this.error.set(e?.error?.description ?? 'تعذر تحميل البيانات');
        this.loading.set(false);
      },
    });
  }

  // ---- interactions ----
  onSearch(value: string): void { this.search.set(value); this.page.set(1); }

  toggleSort(col: ColumnDef): void {
    if (col.sortable === false) return;
    const cur = this.sort();
    if (!cur || cur.field !== col.key) this.sort.set({ field: col.key, desc: false });
    else if (!cur.desc) this.sort.set({ field: col.key, desc: true });
    else this.sort.set(null);
    this.page.set(1);
  }

  sortIcon(col: ColumnDef): string {
    const s = this.sort();
    if (!s || s.field !== col.key) return '';
    return s.desc ? '▼' : '▲';
  }

  setFilter(key: string, value: string): void {
    this.columnFilters.update((m) => ({ ...m, [key]: value }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.columnFilters.set({});
    this.search.set('');
    this.sort.set(null);
    this.page.set(1);
  }

  cellValue(row: Row, col: ColumnDef): string {
    if (col.format) return col.format(row);
    const v = row[col.key];
    return v === null || v === undefined ? '' : String(v);
  }

  // paging
  get totalPages(): number { return this.result()?.totalPages ?? 1; }
  get currentPage(): number { return this.result()?.page ?? 1; }
  prev(): void { if (this.currentPage > 1) this.page.set(this.currentPage - 1); }
  next(): void { if (this.currentPage < this.totalPages) this.page.set(this.currentPage + 1); }

  // column visibility
  toggleColumn(key: string): void {
    this.hidden.update((s) => {
      const next = new Set(s);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  }
  isHidden(key: string): boolean { return this.hidden().has(key); }

  // drag reorder
  onDragStart(key: string): void { this.dragKey = key; }
  onDrop(targetKey: string): void {
    if (!this.dragKey || this.dragKey === targetKey) return;
    const order = [...this.order()];
    const from = order.indexOf(this.dragKey);
    const to = order.indexOf(targetKey);
    order.splice(from, 1);
    order.splice(to, 0, this.dragKey);
    this.order.set(order);
    this.dragKey = null;
  }

  // export
  exportAs(format: ExportFormat): void {
    const src = this.source();
    if (!src.export) return;
    const ext = format === ExportFormat.Xlsx ? 'xlsx' : 'pdf';
    src.export(format, this.query()).subscribe((blob) => downloadBlob(blob, `${this.exportName()}.${ext}`));
  }
  readonly Xlsx = ExportFormat.Xlsx;
  readonly Pdf = ExportFormat.Pdf;
}
