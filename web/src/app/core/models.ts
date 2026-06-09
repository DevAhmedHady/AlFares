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
export interface ExpenseResponse {
  id: string; category: string; amount: number; date: string;
  payee?: string | null; notes?: string | null; createdAtUtc: string; updatedAtUtc: string;
}
export interface CreateExpenseRequest {
  category: string; amount: number; date: string; payee?: string | null; notes?: string | null;
}

// ---- Todos ----
export enum TodoStatus { Open = 0, InProgress = 1, Done = 2 }
export enum TodoPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

export interface TodoResponse {
  id: string; title: string; dueDate: string; status: TodoStatus; priority: TodoPriority;
  notes?: string | null; createdAtUtc: string; updatedAtUtc: string;
}
export interface CreateTodoRequest {
  title: string; dueDate: string; priority: TodoPriority; notes?: string | null;
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
