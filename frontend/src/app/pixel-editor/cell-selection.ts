export interface Cell {
  x: number;
  y: number;
}

// Maps a mouse position to a grid cell, or null if the pointer is off the canvas.
export function cellFromEvent(
  event: MouseEvent,
  canvas: HTMLCanvasElement,
  width: number,
  height: number,
): Cell | null {
  const rect = canvas.getBoundingClientRect();
  if (
    event.clientX < rect.left ||
    event.clientX >= rect.right ||
    event.clientY < rect.top ||
    event.clientY >= rect.bottom
  ) {
    return null;
  }

  const x = Math.floor(((event.clientX - rect.left) / rect.width) * width);
  const y = Math.floor(((event.clientY - rect.top) / rect.height) * height);
  return { x, y };
}
