import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';
import { CreateTournamentComponent } from './components/tournament/create-tournament/create-tournament.component';
import { BracketTestComponent } from './components/bracket-test/bracket-test.component';

const routes: Routes = [
  { path: '', redirectTo: '/tournaments', pathMatch: 'full' },
  { path: 'tournaments', component: TournamentListComponent },
  { path: 'tournament/:id', component: TournamentDetailsComponent },
  { path: 'tournament/:id/bracket', component: BracketTestComponent },
  { path: 'create-tournament', component: CreateTournamentComponent },
  { path: 'bracket-test', component: BracketTestComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
