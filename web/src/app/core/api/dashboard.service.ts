import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../config';
import { ChartDataSourceMetadata, ChartDefinition, ChartRequest, ChartSeries } from '../models';

/** Client for the configurable dashboard: datasources, chart CRUD, data + live preview. */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);
  private get url(): string { return `${this.base}/api/dashboard`; }

  datasources(): Observable<ChartDataSourceMetadata[]> {
    return this.http.get<ChartDataSourceMetadata[]>(`${this.url}/datasources`);
  }
  charts(): Observable<ChartDefinition[]> {
    return this.http.get<ChartDefinition[]>(`${this.url}/charts`);
  }
  create(body: ChartRequest): Observable<ChartDefinition> {
    return this.http.post<ChartDefinition>(`${this.url}/charts`, body);
  }
  update(id: string, body: ChartRequest): Observable<ChartDefinition> {
    return this.http.put<ChartDefinition>(`${this.url}/charts/${id}`, body);
  }
  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/charts/${id}`);
  }
  data(id: string): Observable<ChartSeries> {
    return this.http.get<ChartSeries>(`${this.url}/charts/${id}/data`);
  }
  preview(body: ChartRequest): Observable<ChartSeries> {
    return this.http.post<ChartSeries>(`${this.url}/charts/preview`, body);
  }
}
