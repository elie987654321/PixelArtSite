import { Injectable } from '@angular/core';
import { PixelEdit, Tool, ToolContext } from '../tool';

// Paints a 3x3 block centred on the cursor.
@Injectable()
export class BrushTool implements Tool {
  readonly name = 'brush';

  apply(ctx: ToolContext): PixelEdit[] {
    const pixel = ctx.color + 'ff';
    const edits: PixelEdit[] = [];
    for (let dy = -1; dy <= 1; dy++) {
      for (let dx = -1; dx <= 1; dx++) {
        const x = ctx.x + dx;
        const y = ctx.y + dy;
        if (x >= 0 && x < ctx.width && y >= 0 && y < ctx.height) {
          edits.push({ x, y, pixel });
        }
      }
    }
    return edits;
  }
}
