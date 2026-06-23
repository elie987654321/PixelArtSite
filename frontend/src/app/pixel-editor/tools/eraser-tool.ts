import { Injectable } from '@angular/core';
import { PixelEdit, Tool, ToolContext, TRANSPARENT } from '../tool';

@Injectable()
export class EraserTool implements Tool {
  readonly name = 'eraser';

  getEdits(ctx: ToolContext): PixelEdit[] {
    return [{ x: ctx.x, y: ctx.y, pixel: TRANSPARENT }];
  }
}
