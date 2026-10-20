import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { MatchesApi } from '../../core/api/matches.api';
import { StandingsApi } from '../../core/api/standings.api';
import { TeamsApi } from '../../core/api/teams.api';
import { TournamentsApi } from '../../core/api/tournaments.api';
import { Group, Tournament, TournamentState } from '../../core/models';
import { GameUpdatedEvent, HubEventMap, HubEventName } from '../../core/realtime/hub-events';
import { RealtimeService } from '../../core/realtime/realtime.service';
import { Format, TournamentStatus } from '../../shared/enums';
import { TournamentStore } from './tournament.store';

const TOURNAMENT_ID = 7;

const tournament = { id: TOURNAMENT_ID, name: 'Cup', format: Format.GroupsOnly, status: TournamentStatus.GroupsInProgress } as Tournament;
const state = { currentStatus: TournamentStatus.GroupsInProgress, canScoreMatches: true } as TournamentState;
const group = (id: number, name: string, wins = 0): Group => ({
  id,
  name,
  isFinished: false,
  isSeeded: true,
  version: 0,
  teams: [{ id: 1, name: 'Alpha', wins, losses: 0, points: 0, gameWins: 0, gameLosses: 0, status: 1 }],
  matches: [],
});

// Fake hub: tests push events into it
class FakeRealtime {
  private readonly subjects = new Map<HubEventName, Subject<unknown>>();
  readonly reconnected$ = new Subject<void>();
  joinTournament = vi.fn().mockResolvedValue(undefined);
  leaveTournament = vi.fn().mockResolvedValue(undefined);

  on<K extends HubEventName>(name: K) {
    return this.subject(name).asObservable() as Subject<HubEventMap[K]>;
  }

  emit<K extends HubEventName>(name: K, payload: HubEventMap[K]): void {
    this.subject(name).next(payload);
  }

  private subject(name: HubEventName): Subject<unknown> {
    if (!this.subjects.has(name)) this.subjects.set(name, new Subject());
    return this.subjects.get(name)!;
  }
}

const flushPromises = () => new Promise((resolve) => setTimeout(resolve));

describe('TournamentStore live updates', () => {
  let store: TournamentStore;
  let realtime: FakeRealtime;
  let tournamentsApi: Record<string, ReturnType<typeof vi.fn>>;
  let standingsApi: Record<string, ReturnType<typeof vi.fn>>;

  beforeEach(async () => {
    realtime = new FakeRealtime();
    tournamentsApi = {
      get: vi.fn(() => of(tournament)),
      state: vi.fn(() => of(state)),
      teams: vi.fn(() => of([{ id: 1, name: 'Alpha' }])),
      finalStandings: vi.fn(() => of([])),
    };
    standingsApi = {
      groups: vi.fn(() => of([group(10, 'Group A'), group(11, 'Group B')])),
      bracket: vi.fn(() => of(null)),
    };

    TestBed.configureTestingModule({
      providers: [
        TournamentStore,
        { provide: RealtimeService, useValue: realtime },
        { provide: TournamentsApi, useValue: tournamentsApi },
        { provide: StandingsApi, useValue: standingsApi },
        { provide: TeamsApi, useValue: {} },
        { provide: MatchesApi, useValue: {} },
      ],
    });
    store = TestBed.inject(TournamentStore);
    await store.load(TOURNAMENT_ID);
    store.connect();
    vi.clearAllMocks();
  });

  it('joins the tournament group', () => {
    store.disconnect();
    store.connect();
    expect(realtime.joinTournament).toHaveBeenCalledWith(TOURNAMENT_ID);
  });

  it('ignores tenant events of another tournament', async () => {
    realtime.emit('TeamAdded', { tournamentId: 99, updatedBy: 'bob', teamIds: [5] });
    realtime.emit('TournamentCancelled', { tournamentId: 99, updatedBy: 'bob' });
    realtime.emit('TournamentDeleted', { tournamentId: 99, updatedBy: 'bob' });
    await flushPromises();

    expect(tournamentsApi['teams']).not.toHaveBeenCalled();
    expect(tournamentsApi['get']).not.toHaveBeenCalled();
    expect(store.deletedBy()).toBeNull();
  });

  it('reloads teams when a team of this tournament is added', async () => {
    realtime.emit('TeamAdded', { tournamentId: TOURNAMENT_ID, updatedBy: 'bob', teamIds: [5] });
    await flushPromises();

    expect(tournamentsApi['teams']).toHaveBeenCalledWith(TOURNAMENT_ID);
  });

  it('replaces only the updated group on GameUpdated, without refetching', async () => {
    const updated = group(11, 'Group B', 1);
    realtime.emit('GameUpdated', {
      tournamentId: TOURNAMENT_ID,
      updatedBy: 'bob',
      updatedGroup: updated,
      updatedBracket: null,
      newTournamentStatus: null,
      finalStandings: null,
      tournamentFinished: false,
    } as GameUpdatedEvent);
    await flushPromises();

    expect(store.groups().map((g) => g.teams[0].wins)).toEqual([0, 1]);
    expect(store.groups()[1]).toBe(updated);
    expect(standingsApi['groups']).not.toHaveBeenCalled();
    expect(tournamentsApi['get']).not.toHaveBeenCalled();
  });

  it('reloads the tournament when a game result changes its status', async () => {
    realtime.emit('GameUpdated', {
      tournamentId: TOURNAMENT_ID,
      updatedBy: 'bob',
      updatedGroup: group(10, 'Group A'),
      updatedBracket: null,
      newTournamentStatus: TournamentStatus.Finished,
      finalStandings: [],
      tournamentFinished: true,
    } as unknown as GameUpdatedEvent);
    await flushPromises();

    expect(tournamentsApi['get']).toHaveBeenCalled();
    expect(tournamentsApi['state']).toHaveBeenCalled();
  });

  it('marks the tournament deleted when someone deletes it', async () => {
    realtime.emit('TournamentDeleted', { tournamentId: TOURNAMENT_ID, updatedBy: 'bob' });
    await flushPromises();

    expect(store.deletedBy()).toBe('bob');
  });

  it('reloads everything after a reconnect', async () => {
    realtime.reconnected$.next();
    await flushPromises();

    expect(tournamentsApi['get']).toHaveBeenCalled();
    expect(standingsApi['groups']).toHaveBeenCalled();
    expect(tournamentsApi['teams']).toHaveBeenCalled();
  });

  it('stops applying events after disconnect', async () => {
    store.disconnect();
    realtime.emit('TeamAdded', { tournamentId: TOURNAMENT_ID, updatedBy: 'bob', teamIds: [5] });
    await flushPromises();

    expect(tournamentsApi['teams']).not.toHaveBeenCalled();
    expect(realtime.leaveTournament).toHaveBeenCalledWith(TOURNAMENT_ID);
  });
});
