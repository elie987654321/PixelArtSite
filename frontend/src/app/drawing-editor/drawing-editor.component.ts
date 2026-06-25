import { Component } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PixelEditorComponent } from '../pixel-editor/pixel-editor.component';
import { DrawingService } from '../service/drawing.service';

@Component({
  selector: 'app-drawing-editor',
  standalone: true,
  imports: [PixelEditorComponent, RouterLink],
  templateUrl: './drawing-editor.component.html',
  styleUrl: './drawing-editor.component.css',
})
export class DrawingEditorComponent {
  readonly name: string;
  readonly width: number;
  readonly height: number;

  saving = false;
  saved = false;
  saveError?: string;

  constructor(
    route: ActivatedRoute,
    private readonly drawingService: DrawingService,
  ) {
    const params = route.snapshot.queryParamMap;
    this.name = params.get('name') ?? 'Untitled';
    this.width = Number(params.get('width')) || 16;
    this.height = Number(params.get('height')) || 16;
  }

  onSave(pixels: string[][]): void {
    if (this.saving) return;
    this.saving = true;
    this.saved = false;
    this.saveError = undefined;

    this.drawingService
      .create({ name: this.name, width: this.width, height: this.height, pixels })
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
}
