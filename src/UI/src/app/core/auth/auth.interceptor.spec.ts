import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../api';
import { authInterceptor } from './auth.interceptor';
import { AuthResponse } from './auth.models';
import { refreshTokenStorage } from './refresh-token-storage';
import { SessionStore } from './session.store';

function authResponse(accessToken: string, refreshToken: string): AuthResponse {
  return {
    userId: 1,
    userName: 'alice',
    email: 'alice@example.com',
    tenantId: 1,
    tenantName: 'Org',
    accessToken,
    refreshToken,
    expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  };
}

const flushPromises = () => new Promise((resolve) => setTimeout(resolve));

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let session: SessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionStore);

    session.set(authResponse('old-access', 'refresh-1'));
    refreshTokenStorage.write('refresh-1');
  });

  afterEach(() => {
    backend.verify();
    localStorage.clear();
  });

  it('adds the bearer token to API calls only', () => {
    http.get(apiUrl('tournaments')).subscribe();
    http.get('https://example.com/data').subscribe();

    expect(backend.expectOne(apiUrl('tournaments')).request.headers.get('Authorization')).toBe('Bearer old-access');
    expect(backend.expectOne('https://example.com/data').request.headers.has('Authorization')).toBe(false);
  });

  it('refreshes once for two parallel 401s and retries both with the new token', async () => {
    const first = firstValueFrom(http.get(apiUrl('tournaments')));
    const second = firstValueFrom(http.get(apiUrl('tournaments/1')));

    backend.expectOne(apiUrl('tournaments')).flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(apiUrl('tournaments/1')).flush(null, { status: 401, statusText: 'Unauthorized' });
    await flushPromises();

    const refresh = backend.expectOne(apiUrl('auth/refresh'));
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush(authResponse('new-access', 'refresh-2'));
    await flushPromises();

    const retries = [backend.expectOne(apiUrl('tournaments')), backend.expectOne(apiUrl('tournaments/1'))];
    retries.forEach((r) => expect(r.request.headers.get('Authorization')).toBe('Bearer new-access'));
    retries.forEach((r) => r.flush([]));

    await expect(first).resolves.toEqual([]);
    await expect(second).resolves.toEqual([]);
    expect(refreshTokenStorage.read()).toBe('refresh-2');
  });

  it('signs out when the refresh fails', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const request = firstValueFrom(http.get(apiUrl('tournaments')));

    backend.expectOne(apiUrl('tournaments')).flush(null, { status: 401, statusText: 'Unauthorized' });
    await flushPromises();
    backend.expectOne(apiUrl('auth/refresh')).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(request).rejects.toMatchObject({ status: 401 });
    expect(session.isLoggedIn()).toBe(false);
    expect(refreshTokenStorage.read()).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('does not refresh on a failed login', async () => {
    const login = firstValueFrom(http.post(apiUrl('auth/login'), {}));

    backend.expectOne(apiUrl('auth/login')).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(login).rejects.toMatchObject({ status: 401 });
    backend.expectNone(apiUrl('auth/refresh'));
  });
});
