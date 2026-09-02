import { CalculationPeriod, MetricSourceType } from './enums';

export interface Metric {
  id: number;
  premiumMethodId: number;
  name: string;
  description: string;
  sourceType: MetricSourceType;
  importFrequency: CalculationPeriod;
  isActive: boolean;
  activeFileDefinitionVersion: number | null;
}
