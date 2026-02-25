import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | boolean | UrlTree {

    if (this.authService.isAuthenticated()) {
      return true;
    }

    // Token expired but refresh token may still be valid
    if (this.authService.getRefreshToken()) {
      return this.authService.refreshAccessToken().pipe(
        map(() => true),
        catchError(() => of(this.loginRedirect(state.url)))
      );
    }

    return this.loginRedirect(state.url);
  }

  private loginRedirect(returnUrl: string): UrlTree {
    return this.router.createUrlTree(['/login'], {
      queryParams: { returnUrl }
    });
  }
}
