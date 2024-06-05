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
import { CreateTournamentComponent } from './components/tournament/create-tournament/create-tournament.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DropdownModule } from 'primeng/dropdown';
import { DialogModule } from 'primeng/dialog';
import { ListboxModule } from 'primeng/listbox';
import { MatchesComponent } from './components/matches/matches.component';
import { TableModule } from 'primeng/table';



@NgModule({
  declarations: [
    AppComponent,
    TournamentListComponent,
    FilterByStatusPipe,
    TournamentDetailsComponent,
    GroupComponent,
    BracketComponent,
    NavbarComponent,
    CreateTournamentComponent,
    MatchesComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    CardModule,
    TabViewModule,
    AppRoutingModule,
    MenubarModule,
    ButtonModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    InputTextModule,
    InputTextareaModule,
    DropdownModule,
    DialogModule,
    FormsModule,
    ReactiveFormsModule,
    ListboxModule,
    TableModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
