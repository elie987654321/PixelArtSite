import { Injectable } from '@angular/core';
import { PixelEdit, Tool, ToolContext } from '../tool';

@Injectable()
export class PencilTool implements Tool {
  readonly name = 'pencil';

  getEdits(ctx: ToolContext): PixelEdit[] {
    return [{ x: ctx.x, y: ctx.y, pixel: ctx.color + 'ff' }];
  }
}
