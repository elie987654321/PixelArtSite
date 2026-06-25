import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PixelEditorComponent } from '../pixel-editor/pixel-editor.component';
import { DrawingService } from '../service/drawing.service';

export interface DrawingOptions {
  name: string;
  width: number;
  height: number;
}

@Component({
  selector: 'app-drawing-options',
  standalone: true,
  imports: [FormsModule, PixelEditorComponent],
  templateUrl: './drawing-options.component.html',
  styleUrl: './drawing-options.component.css',
})
export class DrawingOptionsComponent {
  readonly min = 1;
  readonly max = 4096;
  readonly maxNameLength = 100;

  name = '';
  width = 16;
  height = 16;
  error?: string;
  chosen?: DrawingOptions;
  saving = false;
  saved = false;
  saveError?: string;

  constructor(private readonly drawingService: DrawingService) {}

  submit(): void {
    this.error = this.validate();
    if (this.error) return;
    this.chosen = { name: this.name.trim(), width: this.width, height: this.height };
  }

  onSave(pixels: string[][]): void {
    if (!this.chosen || this.saving) return;
    this.saving = true;
    this.saved = false;
    this.saveError = undefined;

    this.drawingService
      .create({
        name: this.chosen.name,
        width: this.chosen.width,
        height: this.chosen.height,
        pixels,
      })
      .subscribe({
        next: () => {
          this.saving = false;
          this.saved = true;
        },
        error: (err) => {
          console.error(err);
          this.saving = false;
          this.saveError = 'Could not save the drawing.';
        },
      });
  }

  private validate(): string | undefined {
    if (this.name.trim().length === 0) {
      return 'Name is required.';
    }
    if (this.name.trim().length > this.maxNameLength) {
      return `Name must be ${this.maxNameLength} characters or fewer.`;
    }
    if (!Number.isInteger(this.width) || !Number.isInteger(this.height)) {
      return 'Width and height must be whole numbers.';
    }
    if (
      this.width  < this.min  ||
      this.width  > this.max  ||
      this.height < this.min  ||
      this.height > this.max
    ) {
      return `Width and height must be between ${this.min} and ${this.max}.`;
    }
    return undefined;
  }
}
