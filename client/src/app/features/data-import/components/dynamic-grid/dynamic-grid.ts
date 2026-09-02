import { Component, computed, input, output, signal } from '@angular/core';
import { FieldFilterOption } from '../../models/field.model';
import { DataFieldFilter, MetricDataRow } from '../../models/metric-data.model';

interface ColumnFilterDraft {
  text: string;
  min: string;
  max: string;
  boolValue: string;
}

export interface SortRequest {
  field: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-dynamic-grid',
  templateUrl: './dynamic-grid.html',
  styleUrl: './dynamic-grid.css',
})
export class DynamicGrid {
  readonly columns = input<FieldFilterOption[]>([]);
  readonly rows = input<MetricDataRow[]>([]);
  readonly totalCount = input(0);
  readonly page = input(1);
  readonly pageSize = input(50);
  readonly loading = input(false);

  readonly filtersChanged = output<DataFieldFilter[]>();
  readonly sortChanged = output<SortRequest | null>();
  readonly pageChanged = output<number>();

  protected readonly sortField = signal<string | null>(null);
  protected readonly sortDir = signal<'asc' | 'desc'>('asc');
  private readonly filterDrafts = signal<Record<string, ColumnFilterDraft>>({});

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  protected draftFor(field: string): ColumnFilterDraft {
    return this.filterDrafts()[field] ?? { text: '', min: '', max: '', boolValue: '' };
  }

  protected setTextFilter(field: string, value: string): void {
    this.updateDraft(field, { text: value });
  }

  protected setRangeFilter(field: string, part: 'min' | 'max', value: string): void {
    this.updateDraft(field, { [part]: value });
  }

  protected setBoolFilter(field: string, value: string): void {
    this.updateDraft(field, { boolValue: value });
  }

  private updateDraft(field: string, patch: Partial<ColumnFilterDraft>): void {
    this.filterDrafts.update((drafts) => ({
      ...drafts,
      [field]: { ...this.draftFor(field), ...patch },
    }));
    this.emitFilters();
  }

  private emitFilters(): void {
    const filters: DataFieldFilter[] = [];
    const drafts = this.filterDrafts();

    for (const column of this.columns()) {
      const draft = drafts[column.systemFieldName];
      if (!draft) continue;

      switch (column.dataType) {
        case 'String':
          if (draft.text.trim()) {
            filters.push({ field: column.systemFieldName, operator: 'contains', value: draft.text.trim() });
          }
          break;
        case 'Number':
        case 'Date':
          if (draft.min || draft.max) {
            filters.push({
              field: column.systemFieldName,
              operator: 'range',
              value: draft.min || null,
              valueTo: draft.max || null,
            });
          }
          break;
        case 'Boolean':
          if (draft.boolValue) {
            filters.push({ field: column.systemFieldName, operator: 'eq', value: draft.boolValue });
          }
          break;
      }
    }

    this.filtersChanged.emit(filters);
  }

  protected onHeaderClick(column: FieldFilterOption): void {
    if (this.sortField() === column.systemFieldName) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortField.set(column.systemFieldName);
      this.sortDir.set('asc');
    }
    this.sortChanged.emit({ field: this.sortField()!, direction: this.sortDir() });
  }

  protected goToPage(target: number): void {
    if (target >= 1 && target <= this.totalPages()) {
      this.pageChanged.emit(target);
    }
  }

  protected formatCellValue(column: FieldFilterOption, row: MetricDataRow): string {
    const value = row.data[column.systemFieldName];
    if (value === null || value === undefined || value === '') {
      return '—';
    }
    if (column.dataType === 'Boolean') {
      return value ? 'כן' : 'לא';
    }
    return String(value);
  }

  protected rowErrorMessage(row: MetricDataRow): string {
    return row.validationErrors.join('; ');
  }
}
