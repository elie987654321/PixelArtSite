
export interface Drawing {
  id: number;
  name: string;
  width: number;
  height: number;
  pixels: string[][];
  createdAt: string;
}

// The fields a client supplies to create a drawing (server sets id/createdAt).
export interface NewDrawing {
  name: string;
  width: number;
  height: number;
  pixels: string[][];
}
