import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-landing',
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent {
  signUpForm: FormGroup;
  errorMessage: string = '';
  isLoading: boolean = false;

  features = [
    {
      icon: 'shield',
      title: 'Multi-Tenant Isolation',
      description: 'Your organization, your data. Complete data isolation ensures your tournaments stay private and secure.'
    },
    {
      icon: 'tournament',
      title: 'Bracket + Groups Support',
      description: 'Run comprehensive tournaments with group stages and playoff brackets, or go straight to elimination.'
    },
    {
      icon: 'auto_awesome',
      title: 'Automated Seeding & Brackets',
      description: 'Automatic bracket generation and team seeding based on group results. No manual work required.'
    }
  ];

  steps = [
    {
      number: '1',
      title: 'Create Organization',
      description: 'Sign up and create your organization in seconds'
    },
    {
      number: '2',
      title: 'Setup Tournament',
      description: 'Configure format, teams, and tournament structure'
    },
    {
      number: '3',
      title: 'Run Matches',
      description: 'Record results and let the system handle the rest'
    }
  ];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.signUpForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      organizationName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  scrollToSignUp(): void {
    const element = document.getElementById('signup-section');
    element?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  onSubmit(): void {
    if (this.signUpForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';

      const { userName, organizationName, email, password } = this.signUpForm.value;

      this.authService.register(userName, email, password, organizationName).subscribe({
        next: () => {
          this.router.navigate(['/tournaments']);
        },
        error: (error) => {
          this.errorMessage = error.error?.message || 'Registration failed. Please try again.';
          this.isLoading = false;
        }
      });
    } else {
      this.markFormGroupTouched(this.signUpForm);
    }
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  getErrorMessage(fieldName: string): string {
    const control = this.signUpForm.get(fieldName);

    if (control?.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }

    if (control?.hasError('email')) {
      return 'Please enter a valid email address';
    }

    if (control?.hasError('minlength')) {
      const minLength = control.errors?.['minlength'].requiredLength;
      return `${this.getFieldLabel(fieldName)} must be at least ${minLength} characters`;
    }

    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      userName: 'User name',
      organizationName: 'Organization name',
      email: 'Email',
      password: 'Password'
    };
    return labels[fieldName] || fieldName;
  }
}
