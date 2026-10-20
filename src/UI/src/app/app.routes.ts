import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { NotFoundPage } from './pages/not-found-page';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'TO2 - Tournament Organizer',
    loadComponent: () => import('./pages/landing-page').then((m) => m.LandingPage),
  },
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
  {
    path: 'tournaments/:id',
    title: 'Tournament - TO2',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tournament/tournament-shell').then((m) => m.TournamentShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', loadComponent: () => import('./features/tournament/tabs/overview-tab').then((m) => m.OverviewTab) },
      { path: 'teams', loadComponent: () => import('./features/tournament/tabs/teams-tab').then((m) => m.TeamsTab) },
      { path: 'groups', loadComponent: () => import('./features/tournament/tabs/groups-tab').then((m) => m.GroupsTab) },
      { path: 'bracket', loadComponent: () => import('./features/tournament/tabs/bracket-tab').then((m) => m.BracketTab) },
      { path: 'results', loadComponent: () => import('./features/tournament/tabs/results-tab').then((m) => m.ResultsTab) },
    ],
  },
  { path: '**', title: 'Not found - TO2', component: NotFoundPage },
];
