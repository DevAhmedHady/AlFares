/** Server-side grid contract mirroring BuildingBlocks.Grids (enums are numeric on the wire). */

export enum GridFilterOp {
  Eq = 0, Neq = 1, Contains = 2, StartsWith = 3,
  Gt = 4, Gte = 5, Lt = 6, Lte = 7, Between = 8, In = 9,
}

export enum GridFieldType { Text = 0, Number = 1, Date = 2, Boolean = 3, Enum = 4 }

export interface GridSort { field: string; desc: boolean; }

export interface GridFilter {
  field: string;
  op: GridFilterOp;
  value?: string | null;
  value2?: string | null;
}

export interface GridQuery {
  page: number;
  pageSize: number;
  search?: string | null;
  sort: GridSort[];
  filters: GridFilter[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export enum ExportFormat { Xlsx = 0, Pdf = 1 }

export function emptyGridQuery(pageSize = 25): GridQuery {
  return { page: 1, pageSize, search: null, sort: [], filters: [] };
}
