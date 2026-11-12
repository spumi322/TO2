import { computed, Injectable, signal } from '@angular/core';
import { AuthResponse } from './auth.models';

export interface Session {
  userId: number;
  userName: string;
  email: string;
  tenantId: number;
  tenantName: string;
  accessToken: string;
  expiresAt: Date;
}

// Who is signed in. The access token lives only here (memory); the refresh token is in localStorage
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly _session = signal<Session | null>(null);

  readonly session = this._session.asReadonly();
  readonly isLoggedIn = computed(() => this._session() !== null);
  readonly accessToken = computed(() => this._session()?.accessToken ?? null);

  set(response: AuthResponse): void {
    this._session.set({
      userId: response.userId,
      userName: response.userName,
      email: response.email,
      tenantId: response.tenantId,
      tenantName: response.tenantName,
      accessToken: response.accessToken,
      expiresAt: new Date(response.expiresAt),
    });
  }

  clear(): void {
    this._session.set(null);
  }
}
