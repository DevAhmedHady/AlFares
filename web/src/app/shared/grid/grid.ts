/* eslint-disable @typescript-eslint/no-explicit-any */
import { CommonModule } from '@angular/common';
import {
  Component, TemplateRef, computed, effect, inject, input, output, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { TooltipModule } from 'primeng/tooltip';
import { AuthStore } from '../../core/auth/auth.store';
import { downloadBlob } from '../../core/api/grid-client';
import {
  ExportFormat, GridFieldType, GridFilter, GridFilterOp, GridQuery, GridSort, PagedResult, emptyGridQuery,
} from '../../core/grid.models';
import { ColumnDef, GridSource } from './grid-column';

type Row = any;

/** Reusable RTL server-side grid built on PrimeNG: sort, global + per-column filter,
 *  column reorder/show-hide, paginator, Excel/PDF export of the full filtered set. */
@Component({
  selector: 'app-grid',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TableModule, ButtonModule, SelectModule, MultiSelectModule,
    InputTextModule, IconFieldModule, InputIconModule, PaginatorModule, TooltipModule,
  ],
  templateUrl: './grid.html',
  styleUrl: './grid.scss',
})
export class GridComponent {
  private readonly store = inject(AuthStore);

  readonly title = input('');
  readonly columns = input<ColumnDef<any>[]>([]);
  readonly source = input.required<GridSource<any>>();
  readonly pageSizeInput = input(25, { alias: 'pageSize' });
  readonly exportName = input('export');
  readonly exportPermission = input<string | null>(null);
  readonly createPermission = input<string | null>(null);
  readonly rowActions = input<TemplateRef<unknown> | null>(null);
  readonly createClicked = output<void>();

  readonly rows = signal<Row[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly search = signal('');
  readonly pageSize = signal(25);
  private readonly sort = signal<GridSort | null>(null);
  private readonly columnFilters = signal<Record<string, string>>({});
  private readonly page = signal(1);

  readonly order = signal<string[]>([]);
  readonly selectedKeys = signal<string[]>([]);

  readonly Field = GridFieldType;
  readonly first = computed(() => (this.page() - 1) * this.pageSize());

  readonly orderedColumns = computed<ColumnDef[]>(() => {
    const byKey = new Map(this.columns().map((c) => [c.key, c]));
    const selected = new Set(this.selectedKeys());
    return this.order().map((k) => byKey.get(k)).filter((c): c is ColumnDef => !!c && selected.has(c.key));
  });

  readonly columnOptions = computed(() => this.columns().map((c) => ({ label: c.header, value: c.key })));
  readonly canExport = computed(() => this.store.has(this.exportPermission()));
  readonly canCreate = computed(() => this.store.has(this.createPermission()));

  constructor() {
    effect(() => {
      const cols = this.columns();
      if (cols.length && this.order().length === 0) {
        this.order.set(cols.map((c) => c.key));
        this.selectedKeys.set(cols.map((c) => c.key));
      }
    });
    effect(() => { this.pageSize.set(this.pageSizeInput()); });
    // Single reactive load whenever the composed query changes.
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
      next: (r: PagedResult<Row>) => { this.rows.set(r.items); this.total.set(r.totalCount); this.loading.set(false); },
      error: (e) => { this.error.set(e?.error?.description ?? 'تعذر تحميل البيانات'); this.loading.set(false); },
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
    if (!s || s.field !== col.key) return 'pi-sort-alt';
    return s.desc ? 'pi-sort-amount-down' : 'pi-sort-amount-up-alt';
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

  onPage(e: PaginatorState): void {
    this.pageSize.set(e.rows ?? this.pageSize());
    this.page.set((e.page ?? 0) + 1);
  }

  onColumnsChange(keys: string[]): void {
    // Preserve declared order; selection drives visibility.
    this.selectedKeys.set(this.columns().map((c) => c.key).filter((k) => keys.includes(k)));
  }

  onColReorder(e: { dragIndex?: number; dropIndex?: number }): void {
    const visible = this.orderedColumns().map((c) => c.key);
    if (e.dragIndex === undefined || e.dropIndex === undefined) return;
    const [moved] = visible.splice(e.dragIndex, 1);
    visible.splice(e.dropIndex, 0, moved);
    const hidden = this.order().filter((k) => !visible.includes(k));
    this.order.set([...visible, ...hidden]);
  }

  exportAs(format: ExportFormat): void {
    const src = this.source();
    if (!src.export) return;
    const ext = format === ExportFormat.Xlsx ? 'xlsx' : 'pdf';
    src.export(format, this.query()).subscribe((blob) => downloadBlob(blob, `${this.exportName()}.${ext}`));
  }

  readonly Xlsx = ExportFormat.Xlsx;
  readonly Pdf = ExportFormat.Pdf;
}
