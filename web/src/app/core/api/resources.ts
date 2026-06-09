import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../config';
import { GridClient } from './grid-client';
import {
  ClientResponse, ExpenseResponse, TodoResponse, UserResponse, TodoStatus, ClientStatus,
} from '../models';
import { PagedResult, GridQuery } from '../grid.models';

@Injectable({ providedIn: 'root' })
export class ClientsService extends GridClient<ClientResponse> {
  private readonly httpClient = inject(HttpClient);
  private readonly base = inject(API_BASE);
  constructor() { super(inject(HttpClient), inject(API_BASE), 'clients'); }
  setStatus(id: string, status: ClientStatus): Observable<ClientResponse> {
    return this.httpClient.put<ClientResponse>(`${this.base}/api/clients/${id}/status`, { status });
  }
}

@Injectable({ providedIn: 'root' })
export class ExpensesService extends GridClient<ExpenseResponse> {
  constructor() { super(inject(HttpClient), inject(API_BASE), 'expenses'); }
}

@Injectable({ providedIn: 'root' })
export class TodosService extends GridClient<TodoResponse> {
  private readonly httpClient = inject(HttpClient);
  private readonly base = inject(API_BASE);
  constructor() { super(inject(HttpClient), inject(API_BASE), 'todos'); }
  changeStatus(id: string, status: TodoStatus): Observable<TodoResponse> {
    return this.httpClient.put<TodoResponse>(`${this.base}/api/todos/${id}/status`, { status });
  }
}

/** Users grid lives under /api/admin/users; only the grid is exposed. */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);
  grid(query: GridQuery): Observable<PagedResult<UserResponse>> {
    return this.http.post<PagedResult<UserResponse>>(`${this.base}/api/admin/users/grid`, query);
  }
}
