import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';

const routes: Routes = [
  { path: '', redirectTo: '/tournaments', pathMatch: 'full' },
  { path: 'tournaments', component: TournamentListComponent },
  { path: 'tournament/:id', component: TournamentDetailsComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
