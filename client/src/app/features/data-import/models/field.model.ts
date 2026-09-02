import { FieldDataType } from './enums';

export interface FieldFilterOption {
  systemFieldName: string;
  excelColumnName: string;
  dataType: FieldDataType;
  isRequired: boolean;
  displayOrder: number;
  availableOperators: string[];
}
