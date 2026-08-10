import { Observable } from 'rxjs';
import { ExportFormat, GridFilter, GridQuery, PagedResult } from '../grid.models';
import { GridSource } from '../../shared/grid/grid-column';

/** Wraps a grid source and appends extra filters to every grid/export query. */
export class ScopedGridSource<T> implements GridSource<T> {
  constructor(
    private readonly inner: GridSource<T>,
    private readonly filters: () => GridFilter[],
  ) {}

  grid(q: GridQuery): Observable<PagedResult<T>> {
    return this.inner.grid({ ...q, filters: [...q.filters, ...this.filters()] });
  }

  export(format: ExportFormat, q: GridQuery): Observable<Blob> {
    if (!this.inner.export) throw new Error('Export unsupported');
    return this.inner.export(format, { ...q, filters: [...q.filters, ...this.filters()] });
  }

  removeMany = (ids: string[]): Observable<{ deleted: number }> => {
    if (!this.inner.removeMany) throw new Error('Bulk delete unsupported');
    return this.inner.removeMany(ids);
  };
}
