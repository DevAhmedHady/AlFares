import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../config';
import { GridClient } from './grid-client';
import {
  ClientResponse, ExpenseResponse, TodoResponse, UserResponse, TodoStatus, ClientStatus, RevenueResponse, CarResponse, WorkerResponse, ExpenseTypeResponse, RevenueTypeResponse, ExpenseScope, OwnerLedgerResponse, OwnerType, OwnerBalance, WorkerReportResponse,
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
  private readonly client=inject(HttpClient);private readonly base=inject(API_BASE);
  constructor() { super(inject(HttpClient), inject(API_BASE), 'expenses'); }
  types(scope?:ExpenseScope):Observable<ExpenseTypeResponse[]>{return this.client.get<ExpenseTypeResponse[]>(`${this.base}/api/expenses/types${scope===undefined?'':`?scope=${scope}`}`);}
}

@Injectable({providedIn:'root'}) export class RevenuesService extends GridClient<RevenueResponse>{private readonly client=inject(HttpClient);private readonly base=inject(API_BASE);constructor(){super(inject(HttpClient),inject(API_BASE),'revenues');}types():Observable<RevenueTypeResponse[]>{return this.client.get<RevenueTypeResponse[]>(`${this.base}/api/revenues/types`);}}
@Injectable({providedIn:'root'}) export class CarsService extends GridClient<CarResponse>{constructor(){super(inject(HttpClient),inject(API_BASE),'cars');}}
@Injectable({providedIn:'root'}) export class WorkersService extends GridClient<WorkerResponse>{private readonly client=inject(HttpClient);private readonly base=inject(API_BASE);constructor(){super(inject(HttpClient),inject(API_BASE),'workers');}advance(id:string,body:unknown){return this.client.post(`${this.base}/api/workers/${id}/advances`,body);}settle(id:string,body:unknown){return this.client.post(`${this.base}/api/workers/${id}/settlements`,body);}report(body:unknown){return this.client.post<WorkerReportResponse>(`${this.base}/api/workers/report`,body);}}
@Injectable({providedIn:'root'}) export class ReportsService{private readonly client=inject(HttpClient);private readonly base=inject(API_BASE);ledger(ownerType:OwnerType,ownerId:string,from?:string,to?:string){return this.client.post<OwnerLedgerResponse>(`${this.base}/api/reports/owner-ledger`,{ownerType,ownerId,from:from||null,to:to||null});}balances(ownerType:OwnerType,ids:string[]){return this.client.post<Record<string,OwnerBalance>>(`${this.base}/api/reports/owner-balances`,{ownerType,ids});}}

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
