import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { BatchHistorySelect } from '../../components/batch-history-select/batch-history-select';
import { DynamicGrid, SortRequest } from '../../components/dynamic-grid/dynamic-grid';
import { ImportHeaderForm, ImportRequest } from '../../components/import-header-form/import-header-form';
import { IMPORT_STATUS_LABELS } from '../../models/enums';
import { FieldFilterOption } from '../../models/field.model';
import { ImportBatchListItem, ImportResult } from '../../models/import.model';
import { DataFieldFilter, MetricDataQuery, MetricDataRow } from '../../models/metric-data.model';
import { Metric } from '../../models/metric.model';
import { DataImportService } from '../../services/data-import.service';

@Component({
  selector: 'app-import-page',
  imports: [ImportHeaderForm, DynamicGrid, BatchHistorySelect],
  templateUrl: './import-page.html',
  styleUrl: './import-page.css',
})
export class ImportPage {
  private readonly dataImportService = inject(DataImportService);

  protected readonly metrics = signal<Metric[]>([]);
  protected readonly metricId = signal<number | null>(null);
  protected readonly columns = signal<FieldFilterOption[]>([]);
  protected readonly batches = signal<ImportBatchListItem[]>([]);
  protected readonly selectedBatchId = signal<number | null>(null);

  protected readonly rows = signal<MetricDataRow[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(50);
  protected readonly filters = signal<DataFieldFilter[]>([]);
  protected readonly sortField = signal<string | null>(null);
  protected readonly sortDir = signal<'asc' | 'desc' | null>(null);
  protected readonly loadingRows = signal(false);

  protected readonly processing = signal(false);
  protected readonly lastImportResult = signal<ImportResult | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly statusLabels = IMPORT_STATUS_LABELS;

  constructor() {
    this.dataImportService.getMetrics().subscribe({
      next: (metrics) => this.metrics.set(metrics),
      error: () => this.errorMessage.set('שגיאה בטעינת רשימת המדדים.'),
    });
  }

  protected onMetricSelected(id: number | null): void {
    this.metricId.set(id);
    this.selectedBatchId.set(null);
    this.page.set(1);
    this.filters.set([]);
    this.sortField.set(null);
    this.sortDir.set(null);
    this.lastImportResult.set(null);
    this.rows.set([]);
    this.totalCount.set(0);
    this.columns.set([]);
    this.batches.set([]);
    this.errorMessage.set(null);

    if (id === null) {
      return;
    }

    this.dataImportService.getMetricFields(id).subscribe({
      next: (fields) => {
        this.columns.set(fields);
        this.loadRows();
      },
      error: () => this.errorMessage.set('לא נמצאה הגדרת מבנה קובץ פעילה עבור מדד זה.'),
    });

    this.loadBatchHistory(id);
  }

  private loadBatchHistory(metricId: number): void {
    this.dataImportService.getImportHistory(metricId).subscribe({
      next: (result) => this.batches.set(result.items),
      error: () => {},
    });
  }

  protected onBatchSelected(batchId: number | null): void {
    this.selectedBatchId.set(batchId);
    this.page.set(1);
    this.loadRows();
  }

  protected onFiltersChanged(filters: DataFieldFilter[]): void {
    this.filters.set(filters);
    this.page.set(1);
    this.loadRows();
  }

  protected onSortChanged(sort: SortRequest | null): void {
    this.sortField.set(sort?.field ?? null);
    this.sortDir.set(sort?.direction ?? null);
    this.loadRows();
  }

  protected onPageChanged(nextPage: number): void {
    this.page.set(nextPage);
    this.loadRows();
  }

  private loadRows(): void {
    const metricId = this.metricId();
    if (metricId === null || this.columns().length === 0) {
      return;
    }

    this.loadingRows.set(true);
    const query: MetricDataQuery = {
      importBatchId: this.selectedBatchId(),
      filters: this.filters(),
      sortField: this.sortField(),
      sortDirection: this.sortDir(),
      page: this.page(),
      pageSize: this.pageSize(),
    };

    this.dataImportService.queryMetricData(metricId, query).subscribe({
      next: (result) => {
        this.rows.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loadingRows.set(false);
      },
      error: () => {
        this.errorMessage.set('שגיאה בטעינת הנתונים.');
        this.loadingRows.set(false);
      },
    });
  }

  protected onProcessImport(request: ImportRequest): void {
    this.processing.set(true);
    this.errorMessage.set(null);

    this.dataImportService.uploadImport(request.metricId, request.year, request.period, request.file).subscribe({
      next: (result) => {
        this.processing.set(false);
        this.lastImportResult.set(result);
        this.selectedBatchId.set(result.batch.id);
        this.page.set(1);
        this.loadBatchHistory(request.metricId);
        this.loadRows();
      },
      error: (err: HttpErrorResponse) => {
        this.processing.set(false);
        this.errorMessage.set(err.error?.message ?? 'שגיאה בעיבוד הקובץ.');
      },
    });
  }
}
