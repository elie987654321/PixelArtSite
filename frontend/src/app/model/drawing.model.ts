
export interface Drawing {
  id: number;
  name: string;
  width: number;
  height: number;
  pixels: string[][];
  createdAt: string;
}

// The client-supplied fields of a drawing, used for both create and update
// (server sets id/createdAt).
export interface DrawingInput {
  name: string;
  width: number;
  height: number;
  pixels: string[][];
}
