import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, map, Observable, tap } from 'rxjs';
import { apiUrl } from '../api';
import {
  AuthResponse,
  InviteCodeResponse,
  LoginRequest,
  OrganizationExistsResponse,
  RegisterRequest,
} from './auth.models';
import { REFRESH_TOKEN_KEY, refreshTokenStorage } from './refresh-token-storage';
import { SessionStore } from './session.store';

// Refresh this long before the access token expires (hub connections, app start)
const EXPIRY_MARGIN_MS = 30_000;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  // One refresh at a time: refresh tokens rotate, so two parallel refreshes would revoke each other
  private refreshing: Promise<boolean> | null = null;

  constructor() {
    // Signed out in another tab: sign out here too
    window.addEventListener('storage', (event) => {
      if (event.key === null || (event.key === REFRESH_TOKEN_KEY && event.newValue === null)) {
        if (this.session.isLoggedIn()) this.sessionExpired();
      }
    });
  }

  login(request: LoginRequest): Observable<void> {
    return this.http.post<AuthResponse>(apiUrl('auth/login'), request).pipe(
      tap((response) => this.startSession(response)),
      map(() => undefined),
    );
  }

  register(request: RegisterRequest): Observable<void> {
    return this.http.post<AuthResponse>(apiUrl('auth/register'), request).pipe(
      tap((response) => this.startSession(response)),
      map(() => undefined),
    );
  }

  organizationExists(name: string): Observable<boolean> {
    return this.http
      .get<OrganizationExistsResponse>(apiUrl('auth/organizations/exists'), { params: { name } })
      .pipe(map((response) => response.exists));
  }

  generateInviteCode(): Observable<InviteCodeResponse> {
    return this.http.post<InviteCodeResponse>(apiUrl('tenant/invite-code'), {});
  }

  // Revokes the refresh token on the server; the local session ends even if that fails
  async logout(): Promise<void> {
    const refreshToken = refreshTokenStorage.read();
    if (refreshToken && this.session.isLoggedIn()) {
      try {
        await firstValueFrom(this.http.post(apiUrl('auth/revoke'), { refreshToken }));
      } catch {
        // already revoked or offline
      }
    }
    this.endSession();
    await this.router.navigate(['/login']);
  }

  // App start: the access token is in memory only, so a reload needs a refresh
  async restore(): Promise<void> {
    if (refreshTokenStorage.read()) await this.refresh();
  }

  refresh(): Promise<boolean> {
    this.refreshing ??= this.doRefresh().finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  // For callers that can't retry on 401 (SignalR): a token that is valid for a while
  async freshAccessToken(): Promise<string | null> {
    const session = this.session.session();
    if (session && session.expiresAt.getTime() - Date.now() > EXPIRY_MARGIN_MS) return session.accessToken;
    return (await this.refresh()) ? this.session.accessToken() : null;
  }

  // Refresh failed for good: back to the login page, then to where the user was
  sessionExpired(): void {
    this.endSession();
    const returnUrl = this.router.url;
    void this.router.navigate(['/login'], {
      queryParams: returnUrl.startsWith('/login') ? {} : { returnUrl },
    });
  }

  private async doRefresh(): Promise<boolean> {
    const token = refreshTokenStorage.read();
    if (!token) return false;

    try {
      this.startSession(await this.postRefresh(token));
      return true;
    } catch (error) {
      if (!isRejected(error)) return false; // offline, 429, 5xx: keep the token, try again later

      // Another tab may have rotated the token meanwhile: retry once with the stored newer one
      const latest = refreshTokenStorage.read();
      if (latest && latest !== token) {
        try {
          this.startSession(await this.postRefresh(latest));
          return true;
        } catch {
          // fall through
        }
      }
      this.endSession();
      return false;
    }
  }

  private postRefresh(refreshToken: string): Promise<AuthResponse> {
    return firstValueFrom(this.http.post<AuthResponse>(apiUrl('auth/refresh'), { refreshToken }));
  }

  private startSession(response: AuthResponse): void {
    this.session.set(response);
    refreshTokenStorage.write(response.refreshToken);
  }

  private endSession(): void {
    this.session.clear();
    refreshTokenStorage.remove();
  }
}

// The server refused the token (invalid, revoked, or used by a parallel refresh)
function isRejected(error: unknown): boolean {
  return error instanceof HttpErrorResponse && [400, 401, 409].includes(error.status);
}
