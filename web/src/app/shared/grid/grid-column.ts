import { GridFieldType } from '../../core/grid.models';

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

/** A data source the grid can page + export. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export interface GridSource<T = any> {
  grid(query: import('../../core/grid.models').GridQuery):
    import('rxjs').Observable<import('../../core/grid.models').PagedResult<T>>;
  export?(format: import('../../core/grid.models').ExportFormat,
    grid: import('../../core/grid.models').GridQuery): import('rxjs').Observable<Blob>;
}
