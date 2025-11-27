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
// Angular Material Imports
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatListModule } from '@angular/material/list';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRadioModule } from '@angular/material/radio';
import { LandingComponent } from './components/landing/landing.component';
import { CreateTournamentWizardComponent } from './components/tournament/create-tournament-wizard/create-tournament-wizard.component';
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
    CreateTournamentWizardComponent,
    ConfirmDialogComponent
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
    ListboxModule,
    TableModule,
    PasswordModule,
    MessageModule,
    // Angular Material Modules
    MatCardModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTabsModule,
    MatIconModule,
    MatProgressBarModule,
    MatListModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatRadioModule

  ],
  providers: [
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
