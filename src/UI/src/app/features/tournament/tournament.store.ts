import { computed, inject, Injectable, signal } from '@angular/core';
import { filter, firstValueFrom, forkJoin, map, merge, Observable, Subscription } from 'rxjs';
import { MatchesApi } from '../../core/api/matches.api';
import { StandingsApi } from '../../core/api/standings.api';
import { TeamsApi } from '../../core/api/teams.api';
import { TournamentsApi } from '../../core/api/tournaments.api';
import { toApiError } from '../../core/http/api-error';
import {
  Bracket,
  GameResult,
  Group,
  StandingGame,
  StandingMatch,
  Team,
  TeamPlacement,
  Tournament,
  TournamentState,
} from '../../core/models';
import { GameUpdatedEvent, HubEventName } from '../../core/realtime/hub-events';
import { RealtimeService } from '../../core/realtime/realtime.service';

/**
 * State of one open tournament page, provided on the shell route (one instance per open tournament).
 * Loads everything in parallel; reloads replace single signals, so the open tab is never rebuilt.
 * Actions call the API, then reload what changed.
 */
@Injectable()
export class TournamentStore {
  private readonly tournamentsApi = inject(TournamentsApi);
  private readonly teamsApi = inject(TeamsApi);
  private readonly standingsApi = inject(StandingsApi);
  private readonly matchesApi = inject(MatchesApi);
  private readonly realtime = inject(RealtimeService);
  private liveSubscription: Subscription | null = null;

  private readonly _id = signal(0);
  readonly id = this._id.asReadonly();

  readonly tournament = signal<Tournament | null>(null);
  readonly state = signal<TournamentState | null>(null);
  readonly teams = signal<Team[]>([]);
  readonly groups = signal<Group[]>([]);
  readonly bracket = signal<Bracket | null>(null);
  readonly results = signal<TeamPlacement[]>([]);

  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly loadError = signal<string | null>(null);
  // Set when the tournament was deleted while open (by name of who deleted it)
  readonly deletedBy = signal<string | null>(null);

  // Match and game DTOs carry team ids only
  readonly teamNames = computed(() => new Map(this.teams().map((team) => [team.id, team.name])));

  async load(id: number): Promise<void> {
    this._id.set(id);
    this.loading.set(true);
    try {
      const data = await firstValueFrom(
        forkJoin({
          tournament: this.tournamentsApi.get(id),
          state: this.tournamentsApi.state(id),
          teams: this.tournamentsApi.teams(id),
          groups: this.standingsApi.groups(id),
          bracket: this.standingsApi.bracket(id),
          results: this.tournamentsApi.finalStandings(id),
        }),
      );
      this.tournament.set(data.tournament);
      this.state.set(data.state);
      this.teams.set(data.teams);
      this.groups.set(data.groups);
      this.bracket.set(data.bracket);
      this.results.set(data.results);
      this.notFound.set(false);
      this.loadError.set(null);
    } catch (error) {
      const apiError = toApiError(error);
      if (apiError.kind === 'notFound') this.notFound.set(true);
      else this.loadError.set(apiError.message);
    } finally {
      this.loading.set(false);
    }
  }

  reload(): Promise<void> {
    return this.load(this._id());
  }

  async reloadTournament(): Promise<void> {
    const id = this._id();
    const data = await firstValueFrom(
      forkJoin({ tournament: this.tournamentsApi.get(id), state: this.tournamentsApi.state(id) }),
    );
    this.tournament.set(data.tournament);
    this.state.set(data.state);
  }

  async reloadTeams(): Promise<void> {
    const id = this._id();
    const [teams] = await Promise.all([firstValueFrom(this.tournamentsApi.teams(id)), this.reloadTournament()]);
    this.teams.set(teams);
  }

  async reloadStandings(): Promise<void> {
    const id = this._id();
    const data = await firstValueFrom(
      forkJoin({ groups: this.standingsApi.groups(id), bracket: this.standingsApi.bracket(id) }),
    );
    this.groups.set(data.groups);
    this.bracket.set(data.bracket);
  }

  async reloadResults(): Promise<void> {
    this.results.set(await firstValueFrom(this.tournamentsApi.finalStandings(this._id())));
  }

  // Replaces one group in place (live update payloads carry the whole changed group)
  applyGroup(group: Group): void {
    this.groups.update((groups) => groups.map((g) => (g.id === group.id ? group : g)));
  }

  applyBracket(bracket: Bracket): void {
    this.bracket.set(bracket);
  }

  // Live updates: join the tournament's hub group and apply events while the page is open
  connect(): void {
    this.disconnect();
    const id = this._id();
    this.liveSubscription = this.liveUpdates().subscribe((apply) =>
      apply().catch((error: unknown) => console.warn('Live update failed', error)),
    );
    this.realtime.joinTournament(id).catch(() => this.notFound.set(true));
  }

  disconnect(): void {
    if (!this.liveSubscription) return;
    this.liveSubscription.unsubscribe();
    this.liveSubscription = null;
    void this.realtime.leaveTournament(this._id());
  }

  applyGameUpdated(event: GameUpdatedEvent): Promise<void> {
    if (event.updatedGroup) this.applyGroup(event.updatedGroup);
    if (event.updatedBracket) this.applyBracket(event.updatedBracket);
    if (event.finalStandings) this.results.set(event.finalStandings);

    const reloads: Promise<void>[] = [];
    if (event.newTournamentStatus !== null) reloads.push(this.reloadTournament());
    if (event.tournamentFinished && !event.finalStandings) reloads.push(this.reloadResults());
    return Promise.all(reloads).then(() => undefined);
  }

  // Each event becomes "what to do"; tenant-wide events are filtered to this tournament
  private liveUpdates(): Observable<() => Promise<void>> {
    const mine = <K extends HubEventName>(name: K) =>
      this.realtime.on(name).pipe(filter((event) => event.tournamentId === this._id()));

    return merge(
      mine('GameUpdated').pipe(map((event) => () => this.applyGameUpdated(event))),
      merge(mine('TeamAdded'), mine('TeamRemoved')).pipe(map(() => () => this.reloadTeams())),
      merge(mine('TournamentUpdated'), mine('TournamentCancelled')).pipe(map(() => () => this.reloadTournament())),
      merge(mine('GroupsStarted'), mine('BracketStarted')).pipe(
        map(() => () => Promise.all([this.reloadTournament(), this.reloadStandings()]).then(() => undefined)),
      ),
      mine('TournamentDeleted').pipe(map((event) => async () => this.deletedBy.set(event.updatedBy))),
      // Events may have been missed while offline
      this.realtime.reconnected$.pipe(map(() => () => this.reload())),
    );
  }

  // Actions: errors propagate to the caller, which shows them

  async startGroups(): Promise<void> {
    await firstValueFrom(this.tournamentsApi.startGroups(this._id()));
    await Promise.all([this.reloadTournament(), this.reloadStandings()]);
  }

  async startBracket(): Promise<void> {
    await firstValueFrom(this.tournamentsApi.startBracket(this._id()));
    await Promise.all([this.reloadTournament(), this.reloadStandings()]);
  }

  async cancel(reason: string | null): Promise<void> {
    await firstValueFrom(this.tournamentsApi.cancel(this._id(), reason));
    await this.reloadTournament();
  }

  async delete(): Promise<void> {
    await firstValueFrom(this.tournamentsApi.delete(this._id()));
  }

  async addTeams(names: string[]): Promise<Team[]> {
    const added = await firstValueFrom(this.tournamentsApi.addTeams(this._id(), names));
    await this.reloadTeams();
    return added;
  }

  async removeTeam(teamId: number): Promise<void> {
    await firstValueFrom(this.teamsApi.removeFromTournament(teamId, this._id()));
    await this.reloadTeams();
  }

  // Sets the winner of one game (new or a correction); sends the game version the page loaded
  async setGameWinner(match: StandingMatch, game: StandingGame, winnerId: number): Promise<GameResult> {
    try {
      const result = await firstValueFrom(
        this.matchesApi.setGameResult({
          gameId: game.id,
          winnerId,
          teamAScore: null,
          teamBScore: null,
          matchId: match.id,
          standingId: match.standingId,
          tournamentId: this._id(),
          version: game.version,
        }),
      );
      await this.afterGameResult(result);
      return result;
    } catch (error) {
      // 409: someone else changed it, or the state moved on: show fresh data
      if (toApiError(error).kind === 'conflict') await Promise.all([this.reloadStandings(), this.reloadTournament()]);
      throw error;
    }
  }

  private async afterGameResult(result: GameResult): Promise<void> {
    const reloads: Promise<void>[] = [this.reloadStandings()];
    if (result.newTournamentStatus !== null) reloads.push(this.reloadTournament());
    if (result.finalStandings) this.results.set(result.finalStandings);
    else if (result.tournamentFinished) reloads.push(this.reloadResults());
    await Promise.all(reloads);
  }
}
