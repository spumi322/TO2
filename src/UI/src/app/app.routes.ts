import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { NotFoundPage } from './pages/not-found-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tournaments' },
  {
    path: 'login',
    title: 'Sign in - TO2',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create account - TO2',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'tournaments',
    title: 'Tournaments - TO2',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tournaments/tournament-list-page').then((m) => m.TournamentListPage),
  },
  {
    path: 'tournaments/new',
    title: 'New tournament - TO2',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tournaments/create-tournament-page').then((m) => m.CreateTournamentPage),
  },
  { path: '**', title: 'Not found - TO2', component: NotFoundPage },
];
