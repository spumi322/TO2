import { effect, inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { filter, map, Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { SessionStore } from '../auth/session.store';
import { HUB_EVENT_NAMES, HubEventMap, HubEventName } from './hub-events';

export type ConnectionState = 'connected' | 'reconnecting' | 'disconnected';

// After automatic reconnect gives up (or the first start fails), try again this often
const RESTART_DELAY_MS = 10_000;

/**
 * One hub connection per signed-in session: started on sign-in/session restore, stopped on sign-out.
 * Pages subscribe to typed event streams; the tournament page joins its tournament's group.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);
  private readonly session = inject(SessionStore);

  readonly state = signal<ConnectionState>('disconnected');

  private readonly events = new Subject<{ name: HubEventName; payload: unknown }>();
  private readonly reconnectedSubject = new Subject<void>();
  // Fires after a reconnect (groups rejoined): events may have been missed, so reload
  readonly reconnected$ = this.reconnectedSubject.asObservable();

  private connection: HubConnection | null = null;
  private starting: Promise<void> | null = null;
  private restartTimer: ReturnType<typeof setTimeout> | null = null;
  // Tournament groups to rejoin after a reconnect (a new connection id has no groups)
  private readonly joined = new Set<number>();

  constructor() {
    effect(() => {
      if (this.session.isLoggedIn()) void this.start();
      else void this.stop();
    });
  }

  on<K extends HubEventName>(name: K): Observable<HubEventMap[K]> {
    return this.events.pipe(
      filter((event) => event.name === name),
      map((event) => event.payload as HubEventMap[K]),
    );
  }

  // Throws when the hub refuses (unknown tournament or another organization's)
  async joinTournament(tournamentId: number): Promise<void> {
    this.joined.add(tournamentId);
    await this.starting;
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('JoinTournament', tournamentId);
    }
  }

  // Shares a match's draft score with the others on the tournament page (the hub relays it, nothing is saved)
  async shareMatchDraft(tournamentId: number, matchId: number, teamAWins: number, teamBWins: number): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('ShareMatchDraft', tournamentId, matchId, teamAWins, teamBWins);
    }
  }

  async leaveTournament(tournamentId: number): Promise<void> {
    this.joined.delete(tournamentId);
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveTournament', tournamentId).catch(() => undefined);
    }
  }

  private start(): Promise<void> {
    if (this.connection) return this.starting ?? Promise.resolve();

    const connection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: async () => (await this.auth.freshAccessToken()) ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    for (const name of HUB_EVENT_NAMES) {
      connection.on(name, (payload: unknown) => this.events.next({ name, payload }));
    }
    connection.onreconnecting(() => this.state.set('reconnecting'));
    connection.onreconnected(() => void this.afterConnect(true));
    connection.onclose(() => {
      this.state.set('disconnected');
      this.scheduleRestart();
    });

    this.connection = connection;
    this.starting = connection
      .start()
      .then(() => this.afterConnect(false))
      .catch(() => {
        this.state.set('disconnected');
        this.scheduleRestart();
      })
      .finally(() => (this.starting = null));
    return this.starting;
  }

  private async afterConnect(isReconnect: boolean): Promise<void> {
    this.state.set('connected');
    for (const tournamentId of this.joined) {
      await this.connection?.invoke('JoinTournament', tournamentId).catch(() => undefined);
    }
    if (isReconnect) this.reconnectedSubject.next();
  }

  private scheduleRestart(): void {
    if (this.restartTimer || !this.session.isLoggedIn()) return;
    this.restartTimer = setTimeout(() => {
      this.restartTimer = null;
      if (!this.session.isLoggedIn() || this.connection?.state !== HubConnectionState.Disconnected) return;
      void this.connection
        .start()
        .then(() => this.afterConnect(true))
        .catch(() => this.scheduleRestart());
    }, RESTART_DELAY_MS);
  }

  private async stop(): Promise<void> {
    if (this.restartTimer) clearTimeout(this.restartTimer);
    this.restartTimer = null;
    this.joined.clear();

    const connection = this.connection;
    this.connection = null;
    if (connection) {
      await this.starting?.catch(() => undefined);
      await connection.stop();
    }
    this.state.set('disconnected');
  }
}
