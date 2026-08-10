/** Domain DTOs + enums mirroring the API contracts. Enum numeric values match the backend. */

import { GridFieldType, GridFilter } from './grid.models';

// ---- Auth ----
export interface LoginRequest { email: string; password: string; tenantId: string; }
export interface AuthTokens { accessToken: string; refreshToken: string; expiresIn: number; }

// ---- Clients ----
export enum ActivityLevel { Low = 0, Medium = 1, High = 2 }
export enum ClientStatus { Active = 0, Inactive = 1 }

export interface ClientResponse {
  id: string;
  name: string;
  contactName: string;
  phone?: string | null;
  email?: string | null;
  accountBalance: number;
  displayBalance?: number;
  activityLevel: ActivityLevel;
  status: ClientStatus;
  notes?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}
export interface CreateClientRequest {
  name: string; contactName: string; phone?: string | null; email?: string | null;
  accountBalance: number; activityLevel: ActivityLevel; notes?: string | null;
}

// ---- Expenses ----
export enum OwnerType { General = 0, Client = 1, OwnedCar = 2, RentedCar = 3, Worker = 4 }
export enum ExpenseScope { General = 0, Car = 1 }
export interface ExpenseResponse {
  id: string; expenseTypeId?: string | null; expenseTypeName: string; amount: number; date: string;
  payee?: string | null; notes?: string | null; ownerType: OwnerType; ownerId?: string | null; createdAtUtc: string; updatedAtUtc: string;
}
export interface CreateExpenseRequest {
  expenseTypeId?: string | null; amount: number; date: string; payee?: string | null; notes?: string | null; ownerType: OwnerType; ownerId?: string | null;
}
export interface ExpenseTypeResponse { id: string; name: string; scope: ExpenseScope; isActive: boolean; }

export interface ExpenseReportSummary {
  total: number;
  count: number;
  average: number;
  topCategory?: string | null;
  topCategoryAmount: number;
  topCategoryShare: number;
}
export interface ExpenseReportBreakdown { label: string; amount: number; share: number; }
export interface ExpenseReportRow {
  id: string;
  expenseTypeName: string;
  amount: number;
  date: string;
  payee: string;
  notes?: string | null;
}
export interface ExpenseReportResponse {
  summary: ExpenseReportSummary;
  byCategory: ExpenseReportBreakdown[];
  byMonth: ExpenseReportBreakdown[];
  items: ExpenseReportRow[];
  itemsTruncated: boolean;
}
export interface ExpenseReportRequest {
  from?: string | null;
  to?: string | null;
  year?: number | null;
  month?: number | null;
  expenseTypeId?: string | null;
}

// ---- Revenues ----
export interface RevenueResponse { id:string; revenueTypeId?:string|null; revenueTypeName:string; amount:number; date:string; source:string; notes?:string|null; ownerType:OwnerType; ownerId?:string|null; createdAtUtc:string; updatedAtUtc:string; }
export interface CreateRevenueRequest {
  revenueTypeId?: string | null; amount: number; date: string; source: string; notes?: string | null; ownerType: OwnerType; ownerId?: string | null;
}
export interface RevenueTypeResponse { id:string; name:string; isActive:boolean; }

export interface RevenueReportSummary {
  total: number;
  count: number;
  average: number;
  topCategory?: string | null;
  topCategoryAmount: number;
  topCategoryShare: number;
}
export interface RevenueReportBreakdown { label: string; amount: number; share: number; }
export interface RevenueReportRow {
  id: string;
  revenueTypeName: string;
  amount: number;
  date: string;
  source: string;
  notes?: string | null;
}
export interface RevenueReportResponse {
  summary: RevenueReportSummary;
  byCategory: RevenueReportBreakdown[];
  byMonth: RevenueReportBreakdown[];
  items: RevenueReportRow[];
  itemsTruncated: boolean;
}
export interface RevenueReportRequest {
  from?: string | null;
  to?: string | null;
  year?: number | null;
  month?: number | null;
  revenueTypeId?: string | null;
}

// ---- Cars / Workers / Reports ----
export enum CarType { Owned = 0, Rented = 1 }
export interface CarResponse { id:string; name:string; plateNumber?:string|null; driverName?:string|null; type:CarType; createdAtUtc:string; updatedAtUtc:string; }
export interface WorkerResponse { id:string; name:string; jobTitle?:string|null; isActive:boolean; balance:number; createdAtUtc:string; updatedAtUtc:string; }
export enum LedgerKind { Expense=0, Revenue=1 }
export interface LedgerEntry { id:string; kind:LedgerKind; ownerType:OwnerType; ownerId?:string|null; description:string; amount:number; date:string; }
export interface OwnerLedgerResponse { totalExpenses:number; totalRevenues:number; net:number; entries:LedgerEntry[]; }
export interface OwnerBalance { expenses:number; revenues:number; net:number; }
export interface WorkerTransaction { id:string; date:string; kind:string; amount:number; notes?:string|null; runningBalance:number; }
export interface WorkerReportResponse { totalAdvances:number; totalSettlements:number; balance:number; transactions:WorkerTransaction[]; }

// ---- Todos ----
export enum TodoStatus { Open = 0, InProgress = 1, Done = 2 }
export enum TodoPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

export interface TodoResponse {
  id: string; title: string; dueDate: string; dueTime?: string | null; status: TodoStatus; priority: TodoPriority;
  notes?: string | null; createdAtUtc: string; updatedAtUtc: string;
}
export interface CreateTodoRequest {
  title: string; dueDate: string; dueTime?: string | null; priority: TodoPriority; notes?: string | null;
}

// ---- Users ----
export interface UserResponse {
  id: string; email: string; displayName: string; isActive: boolean; createdAtUtc: string;
}

// ---- Dashboard / Charts ----
export enum ChartType { Bar = 0, Pie = 1, Line = 2 }
export enum ChartAggregation { Count = 0, Sum = 1, Avg = 2, Min = 3, Max = 4 }

export interface ChartFieldDescriptor {
  key: string; displayName: string; type: GridFieldType; canGroupBy: boolean; canAggregate: boolean;
}
export interface ChartDataSourceMetadata {
  key: string; displayName: string; fields: ChartFieldDescriptor[];
}
export interface ChartPoint { label: string; value: number; }
export interface ChartSeries { name: string; points: ChartPoint[]; }

export interface ChartDefinition {
  id: string;
  title: string;
  type: ChartType;
  datasourceKey: string;
  xField: string;
  yField?: string | null;
  aggregation: ChartAggregation;
  colorsJson: string;
  filtersJson?: string | null;
  layoutOrder: number;
  isEnabled: boolean;
}
export interface ChartRequest {
  title: string;
  type: ChartType;
  datasourceKey: string;
  xField: string;
  yField?: string | null;
  aggregation: ChartAggregation;
  colorsJson: string;
  filtersJson?: string | null;
  layoutOrder: number;
}

// ---- API errors ----
export interface ApiError { code: string; description: string; }
export type { GridFilter };
