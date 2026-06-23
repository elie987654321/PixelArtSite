import {
  AfterViewInit,
  Component,
  ElementRef,
  Inject,
  Input,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Tool, TOOLS, TRANSPARENT } from './tool';
import { Cell, cellFromClick, lineCells } from './cell-selection';
import { PencilTool } from './tools/pencil-tool';
import { EraserTool } from './tools/eraser-tool';
import { BrushTool } from './tools/brush-tool';

@Component({
  selector: 'app-pixel-editor',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './pixel-editor.component.html',
  styleUrl: './pixel-editor.component.css',
  providers: 
  [
    { provide: TOOLS, useClass: PencilTool, multi: true },
    { provide: TOOLS, useClass: EraserTool, multi: true },
    { provide: TOOLS, useClass: BrushTool, multi: true }
  ],
})
export class PixelEditorComponent implements AfterViewInit {
  @Input({ required: true }) width!: number;
  @Input({ required: true }) height!: number;

  @ViewChild('canvas', { static: true })
  private canvasRef!: ElementRef<HTMLCanvasElement>;

  private ctx!: CanvasRenderingContext2D;
  private strokeInProgress = false;
  private lastCell: Cell | null = null;

  grid: string[][] = [];
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

  beginStroke(event: MouseEvent): void {
    const cell = cellFromClick(event, this.canvasRef.nativeElement, this.width, this.height);
    if (!cell) return;
    this.strokeInProgress = true;
    this.lastCell = cell;
    this.applyToolAt(cell);
  }

  extendStroke(event: MouseEvent): void {
    if (!this.strokeInProgress) return;
    const cell = cellFromClick(event, this.canvasRef.nativeElement, this.width, this.height);
    if (!cell) return;

    const from = this.lastCell ?? cell;
    for (const c of lineCells(from, cell)) {
      this.applyToolAt(c);
    }
    this.lastCell = cell;
  }

  endStroke(): void {
    this.strokeInProgress = false;
    this.lastCell = null;
  }

  private applyToolAt(cell: Cell): void {
    const edits = this.activeTool.getEdits({
      x: cell.x,
      y: cell.y,
      color: this.color,
      grid: this.grid,
      width: this.width,
      height: this.height,
    });

    for (const edit of edits) {
      this.grid[edit.y][edit.x] = edit.pixel;
      this.ctx.clearRect(edit.x, edit.y,1 ,1);
      this.ctx.fillStyle = edit.pixel;
      this.ctx.fillRect(edit.x, edit.y, 1, 1);
    }
  }

  private makeEmptyGrid(): string[][] {
    return Array.from({ length: this.height }, () =>
      Array.from({ length: this.width }, () => TRANSPARENT),
    );
  }
}
