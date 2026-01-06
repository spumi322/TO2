import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { Tournament, TournamentStateDTO } from '../models/tournament';
import { SignalRService } from './signalr.service';
import { AuthService } from './auth/auth.service';
import { GameUpdatedEvent } from '../models/signalr-events';
import { Standing } from '../models/standing';
import { Match } from '../models/match';

@Injectable({ providedIn: 'root' })
export class TournamentContextService {
  // Tournament and state data
  private tournamentSubject = new BehaviorSubject<Tournament | null>(null);
  private stateSubject = new BehaviorSubject<TournamentStateDTO | null>(null);
  private groupsSubject = new BehaviorSubject<Standing[]>([]);
  private bracketSubject = new BehaviorSubject<Standing | null>(null);
  private currentTournamentId: number | null = null;

  tournament$: Observable<Tournament | null> = this.tournamentSubject.asObservable();
  state$: Observable<TournamentStateDTO | null> = this.stateSubject.asObservable();
  groups$: Observable<Standing[]> = this.groupsSubject.asObservable();
  bracket$: Observable<Standing | null> = this.bracketSubject.asObservable();

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

  setGroups(groups: Standing[]): void {
    // Sort matches by ID to ensure consistent order
    const sortedGroups = groups.map(g => ({
      ...g,
      matches: g.matches ? [...g.matches].sort((a, b) => a.id - b.id) : []
    }));
    this.groupsSubject.next(sortedGroups);
  }

  setBracket(bracket: Standing | null): void {
    if (bracket && bracket.matches) {
      // Sort matches by ID to ensure consistent order
      const sortedBracket = {
        ...bracket,
        matches: [...bracket.matches].sort((a, b) => a.id - b.id)
      };
      this.bracketSubject.next(sortedBracket);
    } else {
      this.bracketSubject.next(bracket);
    }
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
    this.groupsSubject.next([]);
    this.bracketSubject.next(null);
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

  handleGameUpdated(event: GameUpdatedEvent): void {
    // Strategy 1: Full standing replacement (when provided)
    if (event.updatedGroup) {
      // Sort matches by ID to ensure consistent order
      const sortedGroup = {
        ...event.updatedGroup,
        matches: [...event.updatedGroup.matches].sort((a, b) => a.id - b.id)
      };
      const updatedGroups = this.groupsSubject.value.map(g =>
        g.id === sortedGroup.id ? sortedGroup : g
      );
      this.groupsSubject.next(updatedGroups);
    } else if (event.updatedBracket) {
      // Sort matches by ID to ensure consistent order
      const sortedBracket = {
        ...event.updatedBracket,
        matches: [...event.updatedBracket.matches].sort((a, b) => a.id - b.id)
      };
      this.bracketSubject.next(sortedBracket);
    } else if (event.match) {
      // Strategy 2: Minimal patch - replace match only
      this.patchMatchInStandings(event.match);
    } else {
      console.warn('[TournamentContext] GameUpdated event missing both standing and match data:', event);
    }

    // Update tournament status if changed
    if (event.newTournamentStatus !== undefined && event.newTournamentStatus !== null) {
      const currentTournament = this.tournamentSubject.value;
      if (currentTournament) {
        this.tournamentSubject.next({
          ...currentTournament,
          status: event.newTournamentStatus
        });
      }
    }
  }

  private patchMatchInStandings(match: Match): void {
    // Update groups
    const updatedGroups = this.groupsSubject.value.map(group => ({
      ...group,
      matches: group.matches.map(m => m.id === match.id ? match : m)
    }));
    this.groupsSubject.next(updatedGroups);

    // Update bracket
    const bracket = this.bracketSubject.value;
    if (bracket) {
      this.bracketSubject.next({
        ...bracket,
        matches: bracket.matches.map(m => m.id === match.id ? match : m)
      });
    }
  }
}
