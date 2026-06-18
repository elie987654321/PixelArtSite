import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Drawing } from './drawing.model';

@Injectable({ providedIn: 'root' })
export class DrawingService {
  // The API runs on a different origin (hence the CORS policy on the backend).
  // In the browser this must be the host-mapped port, not the in-container hostname.
  private readonly baseUrl = 'http://localhost:5126/api/drawings';

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Drawing[]> {
    return this.http.get<Drawing[]>(this.baseUrl);
  }

  getById(id: number): Observable<Drawing> {
    return this.http.get<Drawing>(`${this.baseUrl}/${id}`);
  }
}
