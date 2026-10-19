import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Subscription, catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit, OnDestroy {
  userName: string = '';
  email: string = '';
  password: string = '';
  tenantName: string = '';
  inviteCode: string = '';
  // True when the typed organization exists: joining it needs an invite code
  organizationExists: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  isLoading: boolean = false;

  private tenantNameChanges = new Subject<string>();
  private tenantNameSubscription?: Subscription;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.tenantNameSubscription = this.tenantNameChanges.pipe(
      map(name => name.trim()),
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(name => name ? this.authService.organizationExists(name).pipe(catchError(() => of(false))) : of(false))
    ).subscribe(exists => this.organizationExists = exists);
  }

  ngOnDestroy(): void {
    this.tenantNameSubscription?.unsubscribe();
  }

  onTenantNameChange(name: string): void {
    this.tenantNameChanges.next(name);
  }

  onSubmit(): void {
    if (!this.userName || !this.email || !this.password || !this.tenantName) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    if (this.organizationExists && !this.inviteCode.trim()) {
      this.errorMessage = 'This organization already exists. Enter an invite code from one of its members.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const inviteCode = this.organizationExists ? this.inviteCode.trim() : undefined;
    this.authService.register(this.userName, this.email, this.password, this.tenantName, inviteCode).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Registration successful! Redirecting...';
        setTimeout(() => {
          this.router.navigate(['/tournaments']);
        }, 1500);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Registration error:', error);

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
        if (error.status === 409) {
          this.errorMessage = errorMsg || 'This email or username is already in use.';
          // Name taken (also by a registration a moment ago): show the invite code field
          if (errorMsg.toLowerCase().includes('invite code')) {
            this.organizationExists = true;
          }
        } else if (error.status === 429) {
          this.errorMessage = errorMsg || 'Too many attempts. Try again in a minute.';
        } else if (error.status === 400) {
          this.errorMessage = errorMsg || 'Invalid registration information. Please check your inputs.';
        } else if (error.status === 0) {
          this.errorMessage = 'Cannot connect to server. Please check your connection.';
        } else {
          this.errorMessage = errorMsg || 'Registration failed. Please try again.';
        }
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
