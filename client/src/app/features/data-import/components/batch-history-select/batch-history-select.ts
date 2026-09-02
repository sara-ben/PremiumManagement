import { Component, input, model } from '@angular/core';
import { ImportBatchListItem } from '../../models/import.model';
import { IMPORT_STATUS_LABELS } from '../../models/enums';

@Component({
  selector: 'app-batch-history-select',
  template: `
    <div class="batch-select">
      <label for="batch-select-input">היסטוריית קליטות</label>
      <select
        id="batch-select-input"
        [value]="selectedBatchId() ?? ''"
        (change)="onChange($any($event.target).value)"
      >
        <option value="">כל השורות של המדד</option>
        @for (batch of batches(); track batch.id) {
          <option [value]="batch.id">
            {{ batch.fileName }} · {{ batch.year }} {{ batch.period }} · {{ statusLabel(batch.status) }} ({{ batch.validRows }}/{{ batch.totalRows }})
          </option>
        }
      </select>
    </div>
  `,
  styles: `
    .batch-select {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    label {
      font-size: 0.875rem;
      font-weight: 600;
    }

    select {
      padding: 0.5rem;
      border: 1px solid var(--border-color, #ccc);
      border-radius: 0.375rem;
      font: inherit;
      max-width: 28rem;
    }
  `,
})
export class BatchHistorySelect {
  readonly batches = input<ImportBatchListItem[]>([]);
  readonly selectedBatchId = model<number | null>(null);

  protected statusLabel(status: keyof typeof IMPORT_STATUS_LABELS): string {
    return IMPORT_STATUS_LABELS[status];
  }

  protected onChange(rawValue: string): void {
    this.selectedBatchId.set(rawValue === '' ? null : Number(rawValue));
  }
}
