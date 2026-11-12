import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { apiUrl, isApiRequest } from '../api';
import { AuthService } from './auth.service';
import { SessionStore } from './session.store';

// Anonymous auth calls: a 401 there means wrong credentials or a dead refresh token, never "refresh and retry"
const NO_RETRY_PATHS = ['auth/login', 'auth/register', 'auth/refresh', 'auth/organizations'].map(apiUrl);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequest(req.url)) return next(req);

  const auth = inject(AuthService);
  const session = inject(SessionStore);
  const withToken = (request: HttpRequest<unknown>) => {
    const token = session.accessToken();
    return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  };

  return next(withToken(req)).pipe(
    catchError((error: unknown) => {
      const retryable =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !NO_RETRY_PATHS.some((path) => req.url.startsWith(path));
      if (!retryable) return throwError(() => error);

      // Refresh once, retry once; a second 401 ends the session
      return from(auth.refresh()).pipe(
        switchMap((refreshed) => {
          if (!refreshed) {
            auth.sessionExpired();
            return throwError(() => error);
          }
          return next(withToken(req)).pipe(
            catchError((retryError: unknown) => {
              if (retryError instanceof HttpErrorResponse && retryError.status === 401) auth.sessionExpired();
              return throwError(() => retryError);
            }),
          );
        }),
      );
    }),
  );
};
