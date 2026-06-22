// A pixel is an #RRGGBBAA hex colour string; "#00000000" is fully transparent.
export type Pixel = string;

export interface Drawing {
  id: number;
  name: string;
  width: number;
  height: number;
  pixels: Pixel[][];
  createdAt: string;
}
