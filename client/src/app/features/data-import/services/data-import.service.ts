import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { FieldFilterOption } from '../models/field.model';
import { MetricDataQuery, MetricDataRow } from '../models/metric-data.model';
import { ImportBatchListItem, ImportResult, PagedResult } from '../models/import.model';
import { Metric } from '../models/metric.model';

const API_BASE = 'http://localhost:5185/api';

@Service()
export class DataImportService {
  private readonly http = inject(HttpClient);

  getMetrics(): Observable<Metric[]> {
    return this.http.get<Metric[]>(`${API_BASE}/metrics`);
  }

  getMetricFields(metricId: number): Observable<FieldFilterOption[]> {
    return this.http.get<FieldFilterOption[]>(`${API_BASE}/metrics/${metricId}/fields`);
  }

  uploadImport(metricId: number, year: number, period: string, file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('metricId', String(metricId));
    formData.append('year', String(year));
    formData.append('period', period);
    formData.append('file', file);
    return this.http.post<ImportResult>(`${API_BASE}/imports`, formData);
  }

  getImportHistory(metricId: number): Observable<PagedResult<ImportBatchListItem>> {
    return this.http.get<PagedResult<ImportBatchListItem>>(`${API_BASE}/import-history`, {
      params: { metricId, page: 1, pageSize: 100 },
    });
  }

  queryMetricData(metricId: number, query: MetricDataQuery): Observable<PagedResult<MetricDataRow>> {
    return this.http.post<PagedResult<MetricDataRow>>(`${API_BASE}/metrics/${metricId}/data/query`, query);
  }
}
