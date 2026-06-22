import { InjectionToken } from '@angular/core';
import { Pixel } from '../model/drawing.model';

export const TRANSPARENT: Pixel = '#00000000';

export interface PixelEdit {
  x: number;
  y: number;
  pixel: Pixel;
}

export interface ToolContext {
  x: number;
  y: number;
  color: string;
  grid: Pixel[][];
  width: number;
  height: number;
}

// A tool decides which pixels to change for one paint interaction at (x, y).
export interface Tool {
  readonly name: string;
  apply(context: ToolContext): PixelEdit[];
}

export const TOOLS = new InjectionToken<Tool[]>('pixel-editor.tools');
