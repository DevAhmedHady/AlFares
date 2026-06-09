import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ExportFormat, GridQuery, PagedResult } from '../grid.models';

/**
 * Reusable typed client for a server-side grid resource. Each resource service composes one of
 * these, pointing at its `/api/<x>` base, and exposes grid paging + filtered export.
 */
export class GridClient<T> {
  constructor(
    private readonly http: HttpClient,
    private readonly apiBase: string,
    private readonly resource: string,
  ) {}

  private get url(): string { return `${this.apiBase}/api/${this.resource}`; }

  grid(query: GridQuery): Observable<PagedResult<T>> {
    return this.http.post<PagedResult<T>>(`${this.url}/grid`, query);
  }

  export(format: ExportFormat, grid: GridQuery): Observable<Blob> {
    return this.http.post(`${this.url}/export`, { format, grid }, { responseType: 'blob' });
  }

  getById(id: string): Observable<T> { return this.http.get<T>(`${this.url}/${id}`); }
  create<TReq>(body: TReq): Observable<T> { return this.http.post<T>(this.url, body); }
  update<TReq>(id: string, body: TReq): Observable<T> { return this.http.put<T>(`${this.url}/${id}`, body); }
  remove(id: string): Observable<void> { return this.http.delete<void>(`${this.url}/${id}`); }
}

/** Triggers a browser download for an exported grid blob. */
export function downloadBlob(blob: Blob, filename: string): void {
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}
