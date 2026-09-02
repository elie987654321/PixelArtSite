import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  username = '';
  password = '';
  submitting = false;
  error?: string;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.submitting) return;

    const trimmedUsername = this.username.trim();
    if (trimmedUsername.length === 0 || this.password.length === 0) {
      this.error = 'Username and password are required.';
      return;
    }

    this.submitting = true;
    this.error = undefined;

    this.authService.login({ username: trimmedUsername, password: this.password }).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err) => {
        console.error(err);
        this.submitting = false;
        this.error =
          err?.status === 401
            ? 'Invalid username or password.'
            : 'Could not sign in. Please try again.';
      },
    });
  }
}
