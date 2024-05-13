import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { FilterByStatusPipe } from './pipes/filter-by-status.pipe';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';
import { GroupComponent } from './components/standing/group/group.component';
import { BracketComponent } from './components/standing/bracket/bracket.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { MenubarModule } from 'primeng/menubar';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TabViewModule } from 'primeng/tabview'; 

@NgModule({
  declarations: [
    AppComponent,
    TournamentListComponent,
    FilterByStatusPipe,
    TournamentDetailsComponent,
    GroupComponent,
    BracketComponent,
    NavbarComponent
  ],
  imports: [
    BrowserModule, HttpClientModule, CardModule, TabViewModule,
    AppRoutingModule, MenubarModule, ButtonModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
