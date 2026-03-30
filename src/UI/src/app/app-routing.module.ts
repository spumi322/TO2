import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';
import { CreateTournamentComponent } from './components/tournament/create-tournament/create-tournament.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { LandingComponent } from './components/landing/landing.component';
import { AuthGuard } from './guards/auth.guard';
import { TeamManagementComponent } from './components/tournament/team-management/team-management.component';

const routes: Routes = [
  { path: '', component: LandingComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'tournaments', component: TournamentListComponent, canActivate: [AuthGuard] },
  { path: 'tournament/:id', component: TournamentDetailsComponent, canActivate: [AuthGuard] },
  { path: 'tournament/:id/team/:teamId', component: TeamManagementComponent, canActivate: [AuthGuard] },
  { path: 'create-tournament', component: CreateTournamentComponent, canActivate: [AuthGuard] }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
