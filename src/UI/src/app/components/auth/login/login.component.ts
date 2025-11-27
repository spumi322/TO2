import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  email: string = '';
  password: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit(): void {
    if (!this.email || !this.password) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/tournaments']);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Login error:', error);

        // Extract error message from various possible formats
        let errorMsg = '';

        // Check for ProblemDetails detail field
        if (error.error?.detail) {
          errorMsg = error.error.detail;
        }
        // Check for validation errors object
        else if (error.error?.errors && typeof error.error.errors === 'object') {
          const errors = error.error.errors;
          const firstError = Object.values(errors)[0];
          errorMsg = Array.isArray(firstError) ? firstError[0] : String(firstError);
        }
        // Check for direct message
        else if (error.error?.message) {
          errorMsg = error.error.message;
        }
        // Check title only if it's not the generic validation message
        else if (error.error?.title && error.error.title !== 'One or more validation errors occurred.') {
          errorMsg = error.error.title;
        }
        // Fallback to HttpErrorResponse message
        else if (error.message) {
          errorMsg = error.message;
        }

        // Set the error message with status code specific handling
        if (error.status === 401) {
          this.errorMessage = 'Invalid email or password. Please try again.';
        } else if (error.status === 403) {
          this.errorMessage = errorMsg || 'Your account is not authorized to access this resource.';
        } else if (error.status === 0) {
          this.errorMessage = 'Cannot connect to server. Please check your connection.';
        } else {
          this.errorMessage = errorMsg || 'Login failed. Please check your credentials.';
        }
      }
    });
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }
}
