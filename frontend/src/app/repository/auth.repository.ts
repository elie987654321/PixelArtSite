import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../model/auth.model';

// Data access for authentication: talks to the API, nothing else.
@Injectable({ providedIn: 'root' })
export class AuthRepository {
  // The API runs on a different origin (hence the CORS policy on the backend).
  // In the browser this must be the host-mapped port, not the in-container hostname.
  private readonly baseUrl = 'http://localhost:5126/api/auth';

  constructor(private readonly http: HttpClient) {}

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials);
  }

  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, credentials);
  }
}
