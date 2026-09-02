export type FieldDataType = 'String' | 'Number' | 'Date' | 'Boolean';

export type ImportStatus = 'Pending' | 'Processing' | 'Success' | 'PartialSuccess' | 'Failed';

export type MetricSourceType = 'Excel' | 'Manual' | 'ExternalApi';

export type CalculationPeriod = 'Monthly' | 'Quarterly' | 'Yearly';

export const IMPORT_STATUS_LABELS: Record<ImportStatus, string> = {
  Pending: 'ממתין',
  Processing: 'בעיבוד',
  Success: 'הושלם בהצלחה',
  PartialSuccess: 'הושלם עם שגיאות',
  Failed: 'נכשל',
};
