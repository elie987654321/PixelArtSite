import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../service/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  username = '';
  password = '';
  confirmPassword = '';
  submitting = false;
  error?: string;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.submitting) return;

    const trimmedUsername = this.username.trim();
    if (trimmedUsername.length === 0) {
      this.error = 'Username is required.';
      return;
    } else if (this.password.length === 0) {
      this.error = 'Password is required.';
      return;
    } else if (this.confirmPassword.length === 0) {
      this.error = 'Please confirm your password.';
      return;
    } else if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }

    this.submitting = true;
    this.error = undefined;

    this.authService
      .register({ username: trimmedUsername, password: this.password })
      .subscribe({
        next: () => this.router.navigateByUrl('/'),
        error: (err) => {
          console.error(err);
          this.submitting = false;
          this.error =
            err?.status === 409
              ? 'That username is already taken.'
              : 'Could not create the account. Please try again.';
        },
      });
  }
}
