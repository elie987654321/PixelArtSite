import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface DrawingOptions {
  name: string;
  width: number;
  height: number;
}

@Component({
  selector: 'app-drawing-options',
  standalone: true,
  imports: [FormsModule],
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

  submit(): void {
    this.error = this.validate();
    if (this.error) return;
    this.chosen = { name: this.name.trim(), width: this.width, height: this.height };
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
