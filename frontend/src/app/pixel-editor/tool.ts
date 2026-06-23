import { InjectionToken } from '@angular/core';

export const TRANSPARENT: string = '#00000000';

export interface PixelEdit {
  x: number;
  y: number;
  pixel: string;
}

export interface ToolContext {
  x: number;
  y: number;
  color: string;
  grid: string[][];
  width: number;
  height: number;
}

// A tool decides which pixels to change for one paint interaction at (x, y).
export interface Tool {
  readonly name: string;
  apply(context: ToolContext): PixelEdit[];
}

export const TOOLS = new InjectionToken<Tool[]>('pixel-editor.tools');
