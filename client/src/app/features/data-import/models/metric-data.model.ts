export interface MetricDataRow {
  rowId: number;
  importBatchId: number;
  rowNumber: number;
  isValid: boolean;
  validationErrors: string[];
  data: Record<string, string | number | boolean | null>;
}

export interface DataFieldFilter {
  field: string;
  operator: string;
  value?: string | null;
  valueTo?: string | null;
}

export interface MetricDataQuery {
  importBatchId?: number | null;
  year?: number | null;
  period?: string | null;
  validOnly?: boolean | null;
  filters: DataFieldFilter[];
  sortField?: string | null;
  sortDirection?: 'asc' | 'desc' | null;
  page: number;
  pageSize: number;
}
