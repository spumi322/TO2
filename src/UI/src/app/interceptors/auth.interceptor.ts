import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Get the access token from auth service
    const accessToken = this.authService.getAccessToken();

    // Don't add token to auth endpoints (login/register)
    const isAuthEndpoint = request.url.includes('/auth/login') ||
                          request.url.includes('/auth/register');

    // Clone the request and add the authorization header if token exists
    if (accessToken && !isAuthEndpoint) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`
        }
      });
    }

    // Handle the request and catch any errors
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // If we get a 401 Unauthorized response, logout and redirect to login
        if (error.status === 401 && !isAuthEndpoint) {
          this.authService.logout();
          this.router.navigate(['/login'], {
            queryParams: { returnUrl: this.router.url }
          });
        }

        return throwError(() => error);
      })
    );
  }
}
