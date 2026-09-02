export interface Cell {
  x: number;
  y: number;
}

// Maps a mouse position to a grid cell, or null if the pointer is off the canvas.
export function cellFromClick(
  event: MouseEvent,
  canvas: HTMLCanvasElement,
  gridWidth: number,
  gridHeight: number,
): Cell | null {
  const rect = canvas.getBoundingClientRect();
  if (
    event.clientX < rect.left ||
    event.clientX >= rect.right ||
    event.clientY < rect.top ||
    event.clientY >= rect.bottom
  ) 
  {
    return null;
  }

  const x = Math.floor(((event.clientX - rect.left) / rect.width) * gridWidth);
  const y = Math.floor(((event.clientY - rect.top) / rect.height) * gridHeight);
  return { x, y };
}

// The cell on the nearest edge, level with the given cell.
export function nearestEdgeCell(cell: Cell, gridWidth: number, gridHeight: number): Cell {
  const toLeft = cell.x;
  const toRight = gridWidth - 1 - cell.x;
  const toTop = cell.y;
  const toBottom = gridHeight - 1 - cell.y;
  const nearest = Math.min(toLeft, toRight, toTop, toBottom);

  let edgeCell: Cell;
  if (nearest === toLeft) {
    edgeCell = { x: 0, y: cell.y };
  } else if (nearest === toRight) {
    edgeCell = { x: gridWidth - 1, y: cell.y };
  } else if (nearest === toTop) {
    edgeCell = { x: cell.x, y: 0 };
  } else {
    edgeCell = { x: cell.x, y: gridHeight - 1 };
  }
  return edgeCell;
}

// All cells on the straight line from `from` to `to` (inclusive), via the DDA
// algorithm — used to fill gaps between fast-moving mouse-move events.
export function lineCells(from: Cell, to: Cell): Cell[] {
  const deltaX = to.x - from.x;
  const deltaY = to.y - from.y;
  const steps = Math.max(Math.abs(deltaX), Math.abs(deltaY));

  if (steps === 0) {
    return [{ x: from.x, y: from.y }];
  }

  const stepX = deltaX / steps;
  const stepY = deltaY / steps;

  const cells: Cell[] = [];
  let x = from.x;
  let y = from.y;
  for (let i = 0; i <= steps; i++) {
    cells.push({ x: Math.round(x), y: Math.round(y) });
    x += stepX;
    y += stepY;
  }
  return cells;
}
