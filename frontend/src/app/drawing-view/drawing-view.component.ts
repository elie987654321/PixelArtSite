import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';
import { Drawing } from '../drawing.model';

@Component({
  selector: 'app-drawing-view',
  standalone: true,
  templateUrl: './drawing-view.component.html',
  styleUrl: './drawing-view.component.css',
})
export class DrawingViewComponent implements AfterViewInit {
  @Input({ required: true }) drawing!: Drawing;

  @ViewChild('canvas', { static: true })
  private canvasRef!: ElementRef<HTMLCanvasElement>;

  error?: string;

  ngAfterViewInit(): void {
    this.render();
  }

  // Paint the drawing's pixel grid onto the canvas, one canvas pixel per cell.
  private render(): void {
    const { width, height, pixels } = this.drawing;

    const canvas = this.canvasRef.nativeElement;
    // Intrinsic size = the real pixel dimensions; CSS scales it up crisply.
    canvas.width = width;
    canvas.height = height;

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      this.error = 'Canvas 2D rendering is unavailable in this browser.';
      return;
    }

    // ImageData is a flat byte array: [r,g,b,a, r,g,b,a, ...] row by row.
    const image = ctx.createImageData(width, height);
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const pixel = pixels[y]?.[x];
        const offset = (y * width + x) * 4;
        image.data[offset] = pixel?.r ?? 0;
        image.data[offset + 1] = pixel?.g ?? 0;
        image.data[offset + 2] = pixel?.b ?? 0;
        // A missing pixel is treated as fully transparent.
        image.data[offset + 3] = pixel?.a ?? 0;
      }
    }
    ctx.putImageData(image, 0, 0);
  }
}
