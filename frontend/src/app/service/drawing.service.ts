import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Drawing, NewDrawing } from '../model/drawing.model';
import { DrawingRepository } from '../repository/drawing.repository';

@Injectable({ providedIn: 'root' })
export class DrawingService {
  constructor(private readonly repository: DrawingRepository) {}

  getAll(): Observable<Drawing[]> {
    return this.repository.getAll();
  }

  getById(id: number): Observable<Drawing> {
    return this.repository.getById(id);
  }

  create(drawing: NewDrawing): Observable<Drawing> {
    return this.repository.create(drawing);
  }
}
