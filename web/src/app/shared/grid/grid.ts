/* eslint-disable @typescript-eslint/no-explicit-any */
import { CommonModule } from '@angular/common';
import {
  Component, OnDestroy, TemplateRef, computed, effect, inject, input, output, signal,
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
import { MessageService } from 'primeng/api';
import { Subscription, finalize } from 'rxjs';
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
export class GridComponent implements OnDestroy {
  private readonly store = inject(AuthStore);
  private readonly messages = inject(MessageService);

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
  readonly exporting = signal<ExportFormat | null>(null);

  readonly search = signal('');
  readonly searchDraft = signal('');
  readonly filterDrafts = signal<Record<string, string>>({});
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
  readonly activeFilterCount = computed(() =>
    Object.values(this.columnFilters()).filter((value) => value !== '').length
    + (this.search() ? 1 : 0)
    + (this.sort() ? 1 : 0));
  private loadSubscription?: Subscription;
  private searchTimer?: ReturnType<typeof setTimeout>;
  private readonly filterTimers = new Map<string, ReturnType<typeof setTimeout>>();

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
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);
    this.loadSubscription = this.source().grid(this.query()).subscribe({
      next: (r: PagedResult<Row>) => { this.rows.set(r.items); this.total.set(r.totalCount); this.loading.set(false); },
      error: (e) => { this.error.set(e?.error?.description ?? 'تعذر تحميل البيانات'); this.loading.set(false); },
    });
  }

  // ---- interactions ----
  onSearch(value: string): void {
    this.searchDraft.set(value);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.search.set(value);
      this.page.set(1);
    }, 300);
  }

  ngOnDestroy(): void {
    this.loadSubscription?.unsubscribe();
    if (this.searchTimer) clearTimeout(this.searchTimer);
    for (const timer of this.filterTimers.values()) clearTimeout(timer);
  }

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

  sortAria(col: ColumnDef): 'ascending' | 'descending' | 'none' {
    const current = this.sort();
    if (!current || current.field !== col.key) return 'none';
    return current.desc ? 'descending' : 'ascending';
  }

  sortLabel(col: ColumnDef): string {
    const direction = this.sortAria(col);
    if (direction === 'ascending') return `ترتيب ${col.header} تنازلياً`;
    if (direction === 'descending') return `إلغاء ترتيب ${col.header}`;
    return `ترتيب ${col.header} تصاعدياً`;
  }

  setFilter(key: string, value: string): void {
    this.filterDrafts.update((drafts) => ({ ...drafts, [key]: value }));
    this.columnFilters.update((m) => ({ ...m, [key]: value }));
    this.page.set(1);
  }

  onFilterInput(key: string, value: string): void {
    this.filterDrafts.update((drafts) => ({ ...drafts, [key]: value }));
    const existing = this.filterTimers.get(key);
    if (existing) clearTimeout(existing);
    this.filterTimers.set(key, setTimeout(() => {
      this.columnFilters.update((filters) => ({ ...filters, [key]: value }));
      this.page.set(1);
      this.filterTimers.delete(key);
    }, 300));
  }

  clearFilters(): void {
    this.columnFilters.set({});
    this.filterDrafts.set({});
    for (const timer of this.filterTimers.values()) clearTimeout(timer);
    this.filterTimers.clear();
    this.search.set('');
    this.searchDraft.set('');
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
    if (keys.length === 0) return;
    this.selectedKeys.set(this.columns().map((c) => c.key).filter((k) => keys.includes(k)));
  }

  filterValue(key: string): string { return this.filterDrafts()[key] ?? this.columnFilters()[key] ?? ''; }

  onColReorder(e: { dragIndex?: number; dropIndex?: number }): void {
    const visible = this.orderedColumns().map((c) => c.key);
    if (e.dragIndex === undefined || e.dropIndex === undefined) return;
    const [moved] = visible.splice(e.dragIndex, 1);
    visible.splice(e.dropIndex, 0, moved);
    const hidden = this.order().filter((k) => !visible.includes(k));
    this.order.set([...visible, ...hidden]);
  }

  moveColumn(key: string, delta: number): void {
    const visible = this.orderedColumns().map((column) => column.key);
    const from = visible.indexOf(key);
    const to = from + delta;
    if (from < 0 || to < 0 || to >= visible.length) return;
    const [moved] = visible.splice(from, 1);
    visible.splice(to, 0, moved);
    const hidden = this.order().filter((columnKey) => !visible.includes(columnKey));
    this.order.set([...visible, ...hidden]);
  }

  exportAs(format: ExportFormat): void {
    const src = this.source();
    if (!src.export || this.exporting() !== null) return;
    const ext = format === ExportFormat.Xlsx ? 'xlsx' : 'pdf';
    this.exporting.set(format);
    src.export(format, this.query()).pipe(finalize(() => this.exporting.set(null))).subscribe({
      next: (blob) => {
        downloadBlob(blob, `${this.exportName()}.${ext}`);
        this.messages.add({ severity: 'success', summary: 'تم تجهيز الملف', detail: `تم تنزيل ${ext.toUpperCase()} بنجاح` });
      },
      error: () => this.messages.add({ severity: 'error', summary: 'تعذر التصدير', detail: 'أعد المحاولة بعد قليل' }),
    });
  }

  readonly Xlsx = ExportFormat.Xlsx;
  readonly Pdf = ExportFormat.Pdf;
}
