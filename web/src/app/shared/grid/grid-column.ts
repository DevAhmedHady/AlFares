import { Observable } from 'rxjs';
import { ExportFormat, GridFieldType, GridQuery, PagedResult } from '../../core/grid.models';

/** Declarative column definition driving the reusable server-side grid. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export interface ColumnDef<T = any> {
  /** Grid field key understood by the API allow-list. */
  key: string;
  /** Arabic header label. */
  header: string;
  /** Field value category (drives the filter editor + parsing). */
  type: GridFieldType;
  /** Whether the column can be sorted (default true). */
  sortable?: boolean;
  /** Whether the column can be filtered (default true). */
  filterable?: boolean;
  /** Optional cell renderer; defaults to the raw value. */
  format?: (row: T) => string;
  /** Optional fixed options for Enum/Boolean filters: [value, label]. */
  options?: [string, string][];
}

/** A data source the grid can page + export (+ optional bulk delete). */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export interface GridSource<T = any> {
  grid(query: GridQuery): Observable<PagedResult<T>>;
  export?(format: ExportFormat, grid: GridQuery): Observable<Blob>;
  removeMany?(ids: string[]): Observable<{ deleted: number }>;
}
