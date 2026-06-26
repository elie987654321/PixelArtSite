import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Drawing, DrawingInput } from '../model/drawing.model';
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

  create(drawing: DrawingInput): Observable<Drawing> {
    return this.repository.create(drawing);
  }

  update(id: number, drawing: DrawingInput): Observable<void> {
    return this.repository.update(id, drawing);
  }
}
