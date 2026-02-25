import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { BehaviorSubject, Subject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { GameUpdatedEvent } from '../models/signalr-events';

// Event interfaces
export interface TournamentUpdateEvent {
  tournamentId: number;
  updatedBy: string;
}

export interface TeamEvent {
  tournamentId: number;
  teamId: number;
  updatedBy: string;
}

export interface MatchEvent {
  tournamentId: number;
  matchId: number;
  updatedBy: string;
}

export interface StandingEvent {
  tournamentId: number;
  standingId: number;
  updatedBy: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: HubConnection | null = null;
  private readonly hubUrl = environment.apiUrl.replace('/api', '/hubs/tournament');

  // Connection state
  private connectionState$ = new BehaviorSubject<HubConnectionState>(HubConnectionState.Disconnected);

  // Event subjects
  private tournamentCreated$ = new Subject<TournamentUpdateEvent>();
  private tournamentUpdated$ = new Subject<TournamentUpdateEvent>();
  private teamAdded$ = new Subject<TeamEvent>();
  private teamRemoved$ = new Subject<TeamEvent>();
  private gameUpdated$ = new Subject<GameUpdatedEvent>();
  private matchUpdated$ = new Subject<MatchEvent>();
  private standingUpdated$ = new Subject<StandingEvent>();
  private groupsStarted$ = new Subject<TournamentUpdateEvent>();
  private bracketStarted$ = new Subject<TournamentUpdateEvent>();

  constructor() {}

  // Public observables
  get connectionState(): Observable<HubConnectionState> {
    return this.connectionState$.asObservable();
  }

  get tournamentCreated(): Observable<TournamentUpdateEvent> {
    return this.tournamentCreated$.asObservable();
  }

  get tournamentUpdated(): Observable<TournamentUpdateEvent> {
    return this.tournamentUpdated$.asObservable();
  }

  get teamAdded(): Observable<TeamEvent> {
    return this.teamAdded$.asObservable();
  }

  get teamRemoved(): Observable<TeamEvent> {
    return this.teamRemoved$.asObservable();
  }

  get gameUpdated(): Observable<GameUpdatedEvent> {
    return this.gameUpdated$.asObservable();
  }

  get matchUpdated(): Observable<MatchEvent> {
    return this.matchUpdated$.asObservable();
  }

  get standingUpdated(): Observable<StandingEvent> {
    return this.standingUpdated$.asObservable();
  }

  get groupsStarted(): Observable<TournamentUpdateEvent> {
    return this.groupsStarted$.asObservable();
  }

  get bracketStarted(): Observable<TournamentUpdateEvent> {
    return this.bracketStarted$.asObservable();
  }

  async startConnection(getAccessToken: () => string | null): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => getAccessToken() || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    // Register event handlers
    this.registerEventHandlers();

    // Connection lifecycle handlers
    this.hubConnection.onreconnecting(() => {
      this.connectionState$.next(HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState$.next(HubConnectionState.Connected);
    });

    this.hubConnection.onclose(() => {
      this.connectionState$.next(HubConnectionState.Disconnected);
    });

    try {
      await this.hubConnection.start();
      this.connectionState$.next(HubConnectionState.Connected);
    } catch (err) {
      console.error('[SignalR] Connection failed:', err);
      this.connectionState$.next(HubConnectionState.Disconnected);
      throw err;
    }
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('TournamentCreated', (data: TournamentUpdateEvent) => {
      this.tournamentCreated$.next(data);
    });

    this.hubConnection.on('TournamentUpdated', (data: TournamentUpdateEvent) => {
      this.tournamentUpdated$.next(data);
    });

    this.hubConnection.on('TeamAdded', (data: TeamEvent) => {
      this.teamAdded$.next(data);
    });

    this.hubConnection.on('TeamRemoved', (data: TeamEvent) => {
      this.teamRemoved$.next(data);
    });

    this.hubConnection.on('GameUpdated', (data: GameUpdatedEvent) => {
      this.gameUpdated$.next(data);
    });

    this.hubConnection.on('MatchUpdated', (data: MatchEvent) => {
      this.matchUpdated$.next(data);
    });

    this.hubConnection.on('StandingUpdated', (data: StandingEvent) => {
      this.standingUpdated$.next(data);
    });

    this.hubConnection.on('GroupsStarted', (data: TournamentUpdateEvent) => {
      this.groupsStarted$.next(data);
    });

    this.hubConnection.on('BracketStarted', (data: TournamentUpdateEvent) => {
      this.bracketStarted$.next(data);
    });
  }

  async joinTournament(tournamentId: number): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('JoinTournament', tournamentId);
      } catch (err) {
        console.error('[SignalR] Failed to join tournament:', err);
      }
    }
  }

  async leaveTournament(tournamentId: number): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveTournament', tournamentId);
      } catch (err) {
        console.error('[SignalR] Failed to leave tournament:', err);
      }
    }
  }

  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        this.connectionState$.next(HubConnectionState.Disconnected);
      } catch (err) {
        console.error('[SignalR] Failed to stop connection:', err);
      }
    }
  }
}
