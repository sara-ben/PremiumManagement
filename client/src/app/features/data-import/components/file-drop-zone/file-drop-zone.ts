import { Component, model, signal } from '@angular/core';

@Component({
  selector: 'app-file-drop-zone',
  host: {
    '(dragover)': 'onDragOver($event)',
    '(dragleave)': 'onDragLeave()',
    '(drop)': 'onDrop($event)',
    class: 'drop-zone-host',
    '[class.drag-over]': 'isDragOver()',
  },
  template: `
    <label class="drop-zone" for="excel-file-input">
      @if (file()) {
        <span class="file-name">📄 {{ file()!.name }}</span>
        <span class="hint">גרור/י קובץ אחר או לחצ/י לבחירה מחדש</span>
      } @else {
        <span class="hint">גרור/י לכאן קובץ Excel (.xlsx/.xls) או לחצ/י לבחירה</span>
      }
    </label>
    <input
      id="excel-file-input"
      type="file"
      accept=".xlsx,.xls"
      class="visually-hidden-input"
      (change)="onFileInputChange($event)"
    />
  `,
  styles: `
    .drop-zone-host {
      display: block;
    }

    .drop-zone {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 2rem 1rem;
      border: 2px dashed var(--border-color, #b3b3b3);
      border-radius: 0.5rem;
      text-align: center;
      cursor: pointer;
      transition: border-color 0.15s ease, background-color 0.15s ease;
    }

    .drag-over .drop-zone {
      border-color: var(--accent-color, #2563eb);
      background-color: var(--accent-bg, #eff6ff);
    }

    .file-name {
      font-weight: 600;
    }

    .hint {
      color: var(--muted-color, #666);
      font-size: 0.875rem;
    }

    .visually-hidden-input {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }
  `,
})
export class FileDropZone {
  readonly file = model<File | null>(null);
  protected readonly isDragOver = signal(false);

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  protected onDragLeave(): void {
    this.isDragOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const droppedFile = event.dataTransfer?.files?.[0];
    if (droppedFile) {
      this.file.set(droppedFile);
    }
  }

  protected onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selectedFile = input.files?.[0];
    if (selectedFile) {
      this.file.set(selectedFile);
    }
  }
}
