import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PixelEditorComponent } from '../pixel-editor/pixel-editor.component';
import { DrawingService } from '../../../core/service/drawing.service';
import { Drawing, DrawingInput } from '../../../core/model/drawing.model';

@Component({
  selector: 'app-existing-drawing-editor-wrapper',
  standalone: true,
  imports: [PixelEditorComponent],
  templateUrl: './drawing-editor.component.html',
  styleUrl: './drawing-editor.component.css',
})
export class ExistingDrawingEditorWrapper implements OnInit {
  drawing?: Drawing;
  loading = true;
  loadError?: string;
  saving = false;
  saved = false;
  saveError?: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly drawingService: DrawingService,
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.loadError = 'Invalid drawing id.';
      this.loading = false;
      return;
    }

    this.drawingService.getById(id).subscribe({
      next: (drawing) => {
        this.drawing = drawing;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loadError =
          err?.status === 404
            ? 'Drawing not found.'
            : 'Could not load the drawing.';
        this.loading = false;
      },
    });
  }

  onSave(pixels: string[][]): void {
    if (!this.drawing || this.saving) return;
    this.saving = true;
    this.saved = false;
    this.saveError = undefined;

    const payload: DrawingInput = {
      name: this.drawing.name,
      width: this.drawing.width,
      height: this.drawing.height,
      pixels,
    };

    this.drawingService.update(this.drawing.id, payload).subscribe({
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
