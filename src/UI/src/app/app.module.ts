import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { TournamentListComponent } from './components/tournament/tournament-list/tournament-list.component';
import { TournamentDetailsComponent } from './components/tournament/tournament-details/tournament-details.component';
import { TournamentHeaderComponent } from './components/tournament/tournament-header/tournament-header.component';
import { TournamentStateBannerComponent } from './components/tournament/tournament-state-banner/tournament-state-banner.component';
import { TournamentInfoCardComponent } from './components/tournament/tournament-info-card/tournament-info-card.component';
import { TeamManagementCardComponent } from './components/tournament/team-management-card/team-management-card.component';
import { RegisteredTeamsListComponent } from './components/tournament/registered-teams-list/registered-teams-list.component';
import { TopResultsCardComponent } from './components/tournament/top-results-card/top-results-card.component';
import { FinalStandingsDisplayComponent } from './components/tournament/final-standings-display/final-standings-display.component';
import { GroupComponent } from './components/standing/group/group.component';
import { BracketComponent } from './components/standing/bracket/bracket.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { MenubarModule } from 'primeng/menubar';
import { MenuModule } from 'primeng/menu';
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
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
// PrimeNG Additional Modules
import { RadioButtonModule } from 'primeng/radiobutton';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ProgressBarModule } from 'primeng/progressbar';
import { StepsModule } from 'primeng/steps';
import { TooltipModule } from 'primeng/tooltip';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { ToastModule } from 'primeng/toast';
import { MessagesModule } from 'primeng/messages';
// PrimeNG Services
import { MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { LandingComponent } from './components/landing/landing.component';
import { ConfirmDialogComponent } from './shared/components/confirm-dialog/confirm-dialog.component';

@NgModule({
  declarations: [
    AppComponent,
    TournamentListComponent,
    TournamentDetailsComponent,
    TournamentHeaderComponent,
    TournamentStateBannerComponent,
    TournamentInfoCardComponent,
    TeamManagementCardComponent,
    RegisteredTeamsListComponent,
    TopResultsCardComponent,
    FinalStandingsDisplayComponent,
    GroupComponent,
    BracketComponent,
    NavbarComponent,
    CreateTournamentComponent,
    MatchesComponent,
    LoginComponent,
    RegisterComponent,
    LandingComponent,
    ConfirmDialogComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    ReactiveFormsModule,
    FormsModule,
    BrowserAnimationsModule,
    // PrimeNG Modules
    ButtonModule,
    CardModule,
    InputTextModule,
    InputTextareaModule,
    DropdownModule,
    DialogModule,
    TabViewModule,
    MenubarModule,
    MenuModule,
    TableModule,
    PasswordModule,
    MessageModule,
    ListboxModule,
    RadioButtonModule,
    ProgressSpinnerModule,
    ProgressBarModule,
    StepsModule,
    TooltipModule,
    FloatLabelModule,
    IconFieldModule,
    InputIconModule,
    ToastModule,
    MessagesModule
  ],
  providers: [
    MessageService,
    DialogService,
    provideAnimationsAsync(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
