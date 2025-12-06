import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { AuthService } from '../../services/auth/auth.service';
import { User } from '../../models/auth/user.model';
import { Subscription } from 'rxjs';
import { TournamentContextService } from '../../services/tournament-context.service';
import { Tournament, TournamentStateDTO, TournamentStatus, Format } from '../../models/tournament';
import { FormatConfig } from '../../utils/format-config';
import { TournamentStatusLabel } from '../tournament/tournament-status-label/tournament-status-label';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit, OnDestroy {
  @ViewChild('userMenu') userMenu!: Menu;

  items: MenuItem[] = [];
  userMenuItems: MenuItem[] = [];
  currentUser: User | null = null;
  private userSubscription?: Subscription;

  // Tournament context
  currentTournament: Tournament | null = null;
  tournamentState: TournamentStateDTO | null = null;
  private tournamentSubscription?: Subscription;
  private stateSubscription?: Subscription;

  // Expose enums for template
  TournamentStatus = TournamentStatus;
  Format = Format;

  constructor(
    private router: Router,
    private authService: AuthService,
    private tournamentContext: TournamentContextService
  ) { }

  ngOnInit(): void {
    // Subscribe to user changes
    this.userSubscription = this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.setupUserMenu();
    });

    // Subscribe to tournament context
    this.tournamentSubscription = this.tournamentContext.tournament$.subscribe(tournament => {
      this.currentTournament = tournament;
    });

    this.stateSubscription = this.tournamentContext.state$.subscribe(state => {
      this.tournamentState = state;
    });
  }

  private setupUserMenu(): void {
    this.userMenuItems = [
      {
        label: 'Logout',
        command: () => {
          this.authService.logout();
        }
      }
    ];
  }

  ngOnDestroy(): void {
    this.userSubscription?.unsubscribe();
    this.tournamentSubscription?.unsubscribe();
    this.stateSubscription?.unsubscribe();
  }

  goToCreateTournament(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/create-tournament']);
  }

  goToLogin(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/login']);
  }

  goToRegister(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/register']);
  }

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.userMenu.toggle(event);
  }

  get isAuthenticated(): boolean {
    return this.currentUser !== null;
  }

  // Tournament info helpers
  getFormatLabel(format: Format): string {
    return FormatConfig.getFormatLabel(format);
  }

  getStatusLabel(status: TournamentStatus): string {
    return TournamentStatusLabel.getLabel(status);
  }

  getStatusClass(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Setup:
        return 'status-setup';
      case TournamentStatus.SeedingGroups:
      case TournamentStatus.SeedingBracket:
        return 'status-seeding';
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.BracketInProgress:
        return 'status-ongoing';
      case TournamentStatus.Finished:
        return 'status-finished';
      case TournamentStatus.Cancelled:
        return 'status-cancelled';
      default:
        return '';
    }
  }

  // Tournament action handlers
  onStartGroups(): void {
    this.tournamentContext.triggerStartGroups();
  }

  onStartTournament(): void {
    this.tournamentContext.triggerStartTournament();
  }

  onStartBracket(): void {
    this.tournamentContext.triggerStartBracket();
  }
}
