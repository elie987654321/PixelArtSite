
// Credentials sent to the API for login and registration.
export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
}

// What the API returns after a successful login/register.
export interface AuthResponse {
  username: string;
  token: string;
}
