import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { FilterByStatusPipe } from './pipes/filter-by-status.pipe';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';

@NgModule({
  declarations: [
    AppComponent,
    TournamentListComponent,
    FilterByStatusPipe,
    TournamentDetailsComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
