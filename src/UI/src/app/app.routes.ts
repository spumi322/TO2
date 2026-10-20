import { Routes } from '@angular/router';
import { NotFoundPage } from './pages/not-found-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tournaments' },
  {
    path: 'login',
    title: 'Sign in - TO2',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create account - TO2',
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'tournaments',
    title: 'Tournaments - TO2',
    loadComponent: () =>
      import('./features/tournaments/tournament-list-page').then((m) => m.TournamentListPage),
  },
  { path: '**', title: 'Not found - TO2', component: NotFoundPage },
];
