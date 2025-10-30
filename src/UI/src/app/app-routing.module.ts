import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';
import { CreateTournamentComponent } from './components/tournament/create-tournament/create-tournament.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: '/tournaments', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'tournaments', component: TournamentListComponent },
  { path: 'tournament/:id', component: TournamentDetailsComponent, canActivate: [AuthGuard] },
  { path: 'create-tournament', component: CreateTournamentComponent, canActivate: [AuthGuard] }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
