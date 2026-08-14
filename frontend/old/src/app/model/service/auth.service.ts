import { computed, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../../auth/core/auth.model';
import { AuthRepository } from '../../auth/repository/auth.repository'; 
// Owns the authenticated session: persists the JWT and exposes reactive state.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly TOKEN_KEY = 'pixelart_token';
  private static readonly USERNAME_KEY = 'pixelart_username';

  // Restored from storage on startup so a refresh keeps the user logged in.
  private readonly _token = signal<string | null>(
    localStorage.getItem(AuthService.TOKEN_KEY),
  );
  private readonly _username = signal<string | null>(
    localStorage.getItem(AuthService.USERNAME_KEY),
  );

  readonly username = this._username.asReadonly();
  readonly isLoggedIn = computed(() => this._token() !== null);

  constructor(private readonly repository: AuthRepository) {}

  // Raw token for the HTTP interceptor to attach to outgoing requests.
  token(): string | null {
    return this._token();
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.repository
      .login(credentials)
      .pipe(tap((res) => this.storeSession(res)));
  }

  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.repository
      .register(credentials)
      .pipe(tap((res) => this.storeSession(res)));
  }

  logout(): void {
    localStorage.removeItem(AuthService.TOKEN_KEY);
    localStorage.removeItem(AuthService.USERNAME_KEY);
    this._token.set(null);
    this._username.set(null);
  }

  private storeSession(res: AuthResponse): void {
    localStorage.setItem(AuthService.TOKEN_KEY, res.token);
    localStorage.setItem(AuthService.USERNAME_KEY, res.username);
    this._token.set(res.token);
    this._username.set(res.username);
  }
}
