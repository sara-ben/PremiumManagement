import { ImportStatus } from './enums';

export interface ImportBatchListItem {
  id: number;
  metricId: number;
  metricName: string;
  fileName: string;
  year: number;
  period: string;
  importDate: string;
  status: ImportStatus;
  totalRows: number;
  validRows: number;
  invalidRows: number;
}

export interface ImportBatchDetail extends ImportBatchListItem {
  metricFileDefinitionId: number;
  fileDefinitionVersion: number;
  errorSummary: string | null;
}

export interface RowValidationError {
  rowNumber: number;
  errors: string[];
}

export interface ImportResult {
  batch: ImportBatchDetail;
  rowErrors: RowValidationError[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
