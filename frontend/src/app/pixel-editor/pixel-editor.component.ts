import {
  AfterViewInit,
  Component,
  ElementRef,
  Inject,
  Input,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Pixel } from '../model/drawing.model';
import { Tool, TOOLS, TRANSPARENT } from './tool';
import { cellFromEvent } from './cell-selection';
import { PencilTool } from './tools/pencil-tool';
import { EraserTool } from './tools/eraser-tool';
import { BrushTool } from './tools/brush-tool';
import { FillTool } from './tools/fill-tool';

@Component({
  selector: 'app-pixel-editor',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './pixel-editor.component.html',
  styleUrl: './pixel-editor.component.css',
  providers: [
    { provide: TOOLS, useClass: PencilTool, multi: true },
    { provide: TOOLS, useClass: EraserTool, multi: true },
    { provide: TOOLS, useClass: BrushTool, multi: true },
    { provide: TOOLS, useClass: FillTool, multi: true },
  ],
})
export class PixelEditorComponent implements AfterViewInit {
  @Input({ required: true }) width!: number;
  @Input({ required: true }) height!: number;

  @ViewChild('canvas', { static: true })
  private canvasRef!: ElementRef<HTMLCanvasElement>;

  private ctx!: CanvasRenderingContext2D;
  private painting = false;

  grid: Pixel[][] = [];
  color = '#e6284a';
  activeTool: Tool;

  constructor(@Inject(TOOLS) readonly tools: Tool[]) {
    this.activeTool = tools[0];
  }

  ngAfterViewInit(): void {
    this.grid = this.makeEmptyGrid();

    const canvas = this.canvasRef.nativeElement;
    canvas.width = this.width;
    canvas.height = this.height;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    this.ctx = ctx;
  }

  selectTool(tool: Tool): void {
    this.activeTool = tool;
  }

  clear(): void {
    this.grid = this.makeEmptyGrid();
    this.ctx.clearRect(0, 0, this.width, this.height);
  }

  startPaint(event: MouseEvent): void {
    this.painting = true;
    this.paintAt(event);
  }

  paintMove(event: MouseEvent): void {
    if (this.painting) this.paintAt(event);
  }

  stopPaint(): void {
    this.painting = false;
  }

  private paintAt(event: MouseEvent): void {
    const cell = cellFromEvent(event, this.canvasRef.nativeElement, this.width, this.height);
    if (!cell) return;

    const edits = this.activeTool.apply({
      x: cell.x,
      y: cell.y,
      color: this.color,
      grid: this.grid,
      width: this.width,
      height: this.height,
    });

    for (const edit of edits) {
      this.grid[edit.y][edit.x] = edit.pixel;
      this.ctx.clearRect(edit.x, edit.y, 1, 1);
      this.ctx.fillStyle = edit.pixel;
      this.ctx.fillRect(edit.x, edit.y, 1, 1);
    }
  }

  private makeEmptyGrid(): Pixel[][] {
    return Array.from({ length: this.height }, () =>
      Array.from({ length: this.width }, () => TRANSPARENT),
    );
  }
}
