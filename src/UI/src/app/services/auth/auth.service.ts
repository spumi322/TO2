import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { User } from '../../models/auth/user.model';
import { LoginRequest } from '../../models/auth/login-request.model';
import { LoginResponse } from '../../models/auth/login-response.model';
import { RegisterRequest } from '../../models/auth/register-request.model';
import { jwtDecode } from 'jwt-decode';

interface JwtPayload {
  sub: string;  // user ID
  email: string;
  name: string;  // userName
  tenantId: string;
  tenantName: string;
  exp: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromToken());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  /**
   * Login user with email and password
   */
  login(email: string, password: string): Observable<LoginResponse> {
    const request: LoginRequest = { email, password };

    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        this.storeTokens(response.accessToken, response.refreshToken);
        const user = this.getUserFromToken();
        this.currentUserSubject.next(user);
      })
    );
  }

  /**
   * Register new user and create tenant
   */
  register(userName: string, email: string, password: string, tenantName: string): Observable<LoginResponse> {
    const request: RegisterRequest = { userName, email, password, tenantName };

    return this.http.post<LoginResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(response => {
        this.storeTokens(response.accessToken, response.refreshToken);
        const user = this.getUserFromToken();
        this.currentUserSubject.next(user);
      })
    );
  }

  /**
   * Logout user and clear tokens
   */
  logout(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  /**
   * Get access token from storage
   */
  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  /**
   * Get refresh token from storage
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Check if user is authenticated (has valid token)
   */
  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      const currentTime = Date.now() / 1000;
      return decoded.exp > currentTime;
    } catch {
      return false;
    }
  }

  /**
   * Get current user from BehaviorSubject
   */
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  /**
   * Store tokens in localStorage
   */
  private storeTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);
  }

  /**
   * Decode JWT token and extract user information
   */
  private getUserFromToken(): User | null {
    const token = this.getAccessToken();
    if (!token) return null;

    try {
      const decoded = jwtDecode<JwtPayload>(token);

      return {
        id: parseInt(decoded.sub),
        userName: decoded.name,
        email: decoded.email,
        tenantId: parseInt(decoded.tenantId),
        tenantName: decoded.tenantName
      };
    } catch {
      return null;
    }
  }
}
