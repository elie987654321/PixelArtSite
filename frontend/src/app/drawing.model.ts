// A single pixel. Alpha controls transparency: 0 = fully transparent, 255 = opaque.
export interface Pixel {
  r: number;
  g: number;
  b: number;
  a: number;
}

// Mirrors the backend Drawing model (the API serializes properties to camelCase).
export interface Drawing {
  id: number;
  name: string;
  width: number;
  height: number;
  // The pixel grid: rows of pixels, pixels[y][x].
  pixels: Pixel[][];
  createdAt: string;
}
