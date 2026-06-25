import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';
import { Drawing } from '../model/drawing.model';

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

  private render(): void {
    const { width, height, pixels } = this.drawing;

    const canvas = this.canvasRef.nativeElement;
    canvas.width = width;
    canvas.height = height;

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      this.error = 'Canvas 2D rendering is unavailable in this browser.';
      return;
    }

    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const hex = pixels[y]?.[x];
        if (!hex) continue;
        ctx.fillStyle = hex;
        ctx.fillRect(x, y, 1, 1);
      }
    }
  }
}
