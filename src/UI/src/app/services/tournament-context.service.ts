import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { Tournament, TournamentStateDTO } from '../models/tournament';
import { SignalRService } from './signalr.service';
import { AuthService } from './auth/auth.service';

@Injectable({ providedIn: 'root' })
export class TournamentContextService {
  // Tournament and state data
  private tournamentSubject = new BehaviorSubject<Tournament | null>(null);
  private stateSubject = new BehaviorSubject<TournamentStateDTO | null>(null);
  private currentTournamentId: number | null = null;

  tournament$: Observable<Tournament | null> = this.tournamentSubject.asObservable();
  state$: Observable<TournamentStateDTO | null> = this.stateSubject.asObservable();

  // Action subjects for navbar buttons
  private startGroupsSubject = new Subject<void>();
  private startTournamentSubject = new Subject<void>();
  private startBracketSubject = new Subject<void>();

  startGroups$ = this.startGroupsSubject.asObservable();
  startTournament$ = this.startTournamentSubject.asObservable();
  startBracket$ = this.startBracketSubject.asObservable();

  private signalRInitialized = false;

  constructor(
    private signalRService: SignalRService,
    private authService: AuthService
  ) {}

  private async ensureSignalRConnection(): Promise<void> {
    if (this.signalRInitialized) return;

    try {
      const token = this.authService.getAccessToken();
      if (!token) {
        console.log('[TournamentContext] No auth token, skipping SignalR initialization');
        return;
      }

      await this.signalRService.startConnection(() => this.authService.getAccessToken());
      this.signalRInitialized = true;
      console.log('[TournamentContext] SignalR connection initialized');
    } catch (err) {
      console.error('[TournamentContext] Failed to initialize SignalR:', err);
      // Don't set initialized flag, allow retry
    }
  }

  // Expose SignalR event observables
  get tournamentCreated$() { return this.signalRService.tournamentCreated; }
  get tournamentUpdated$() { return this.signalRService.tournamentUpdated; }
  get teamAdded$() { return this.signalRService.teamAdded; }
  get teamRemoved$() { return this.signalRService.teamRemoved; }
  get gameUpdated$() { return this.signalRService.gameUpdated; }
  get matchUpdated$() { return this.signalRService.matchUpdated; }
  get standingUpdated$() { return this.signalRService.standingUpdated; }
  get groupsStarted$() { return this.signalRService.groupsStarted; }
  get bracketStarted$() { return this.signalRService.bracketStarted; }
  get connectionState$() { return this.signalRService.connectionState; }

  setTournament(tournament: Tournament | null): void {
    // Update tournament immediately (don't wait for SignalR)
    this.tournamentSubject.next(tournament);

    // Handle SignalR operations asynchronously
    this.handleSignalRForTournament(tournament);
  }

  private async handleSignalRForTournament(tournament: Tournament | null): Promise<void> {
    try {
      // Ensure SignalR is connected
      await this.ensureSignalRConnection();

      // Leave previous tournament if exists
      if (this.currentTournamentId && this.currentTournamentId !== tournament?.id) {
        await this.signalRService.leaveTournament(this.currentTournamentId).catch(err =>
          console.warn('[TournamentContext] Failed to leave tournament:', err)
        );
      }

      // Join new tournament
      if (tournament?.id) {
        this.currentTournamentId = tournament.id;
        await this.signalRService.joinTournament(tournament.id).catch(err =>
          console.warn('[TournamentContext] Failed to join tournament:', err)
        );
      } else {
        this.currentTournamentId = null;
      }
    } catch (err) {
      console.error('[TournamentContext] SignalR operation failed:', err);
    }
  }

  setState(state: TournamentStateDTO | null): void {
    this.stateSubject.next(state);
  }

  clear(): void {
    if (this.currentTournamentId) {
      this.signalRService.leaveTournament(this.currentTournamentId).catch(err =>
        console.error('[TournamentContext] Failed to leave tournament:', err)
      );
      this.currentTournamentId = null;
    }
    this.tournamentSubject.next(null);
    this.stateSubject.next(null);
  }

  triggerStartGroups(): void {
    this.startGroupsSubject.next();
  }

  triggerStartTournament(): void {
    this.startTournamentSubject.next();
  }

  triggerStartBracket(): void {
    this.startBracketSubject.next();
  }
}
