import { Component, computed, input, model, output, signal } from '@angular/core';
import { Metric } from '../../models/metric.model';
import { FileDropZone } from '../file-drop-zone/file-drop-zone';

export interface ImportRequest {
  metricId: number;
  year: number;
  period: string;
  file: File;
}

@Component({
  selector: 'app-import-header-form',
  imports: [FileDropZone],
  template: `
    <div class="header-form">
      <div class="field">
        <label for="metric-select">מדד</label>
        <select
          id="metric-select"
          [value]="metricId() ?? ''"
          (change)="metricId.set(toId($any($event.target).value))"
        >
          <option value="" disabled>בחר/י מדד</option>
          @for (metric of metrics(); track metric.id) {
            <option [value]="metric.id">{{ metric.name }}</option>
          }
        </select>
      </div>

      <div class="field">
        <label for="year-input">שנה</label>
        <input
          id="year-input"
          type="number"
          [value]="year()"
          (input)="year.set(toId($any($event.target).value) ?? year())"
        />
      </div>

      <div class="field">
        <label for="period-input">תקופה</label>
        <input
          id="period-input"
          type="text"
          placeholder="לדוגמה: רבעון 1 או אפריל"
          [value]="period()"
          (input)="period.set($any($event.target).value)"
        />
      </div>

      <div class="field field-file">
        <span class="field-label">קובץ Excel</span>
        <app-file-drop-zone [(file)]="selectedFile" />
      </div>

      <button type="button" class="process-button" [disabled]="!canSubmit() || processing()" (click)="onSubmit()">
        @if (processing()) {
          מעבד...
        } @else {
          עיבוד
        }
      </button>
    </div>
  `,
  styles: `
    .header-form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
      gap: 1rem;
      align-items: end;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .field-file {
      grid-column: span 2;
    }

    label,
    .field-label {
      font-size: 0.875rem;
      font-weight: 600;
    }

    select,
    input[type='number'],
    input[type='text'] {
      padding: 0.5rem;
      border: 1px solid var(--border-color, #ccc);
      border-radius: 0.375rem;
      font: inherit;
    }

    .process-button {
      padding: 0.6rem 1.25rem;
      background: var(--accent-color, #2563eb);
      color: #fff;
      border: none;
      border-radius: 0.375rem;
      font-weight: 600;
      cursor: pointer;
      height: fit-content;
    }

    .process-button:disabled {
      background: var(--disabled-color, #9ca3af);
      cursor: not-allowed;
    }
  `,
})
export class ImportHeaderForm {
  readonly metrics = input<Metric[]>([]);
  readonly processing = input(false);
  readonly process = output<ImportRequest>();

  readonly metricId = model<number | null>(null);
  protected readonly year = signal(new Date().getFullYear());
  protected readonly period = signal('');
  protected readonly selectedFile = signal<File | null>(null);

  protected readonly canSubmit = computed(
    () =>
      this.metricId() !== null &&
      this.year() > 0 &&
      this.period().trim().length > 0 &&
      this.selectedFile() !== null,
  );

  protected toId(raw: string): number | null {
    const value = Number(raw);
    return raw === '' || Number.isNaN(value) ? null : value;
  }

  protected onSubmit(): void {
    if (!this.canSubmit()) {
      return;
    }
    this.process.emit({
      metricId: this.metricId()!,
      year: this.year(),
      period: this.period().trim(),
      file: this.selectedFile()!,
    });
  }
}
