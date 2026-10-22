import { Bracket, Group, StandingMatch, TeamPlacement, Tournament, TournamentState } from '../../../core/models';
import { Format, TournamentStatus } from '../../../shared/enums';
import { stageCount, stageSteps } from '../../tournaments/stage-steps';
import { stageCards, stageCounts, statusCard } from './overview-status';

const tournament = (overrides: Partial<Tournament> = {}): Tournament =>
  ({
    format: Format.GroupsAndBracket,
    status: TournamentStatus.Setup,
    currentTeams: 11,
    maxTeams: 16,
    advancingPerGroup: 2,
    cancelledBy: null,
    cancellationReason: null,
    ...overrides,
  }) as Tournament;

const state = (overrides: Partial<TournamentState> = {}) =>
  ({ canStartGroups: false, canStartBracket: false, ...overrides }) as TournamentState;

const match = (round: number, winnerId: number | null, teams: [number | null, number | null] = [1, 2]): StandingMatch =>
  ({ round, winnerId, teamAId: teams[0], teamBId: teams[1] }) as StandingMatch;

const group = (name: string, isFinished: boolean, matches: StandingMatch[]): Group =>
  ({ name, isFinished, isSeeded: true, matches }) as Group;

// 6 teams in an 8 bracket: round 1 has 2 BYEs (already won) and 2 played matches
const bracket = (r1: (number | null)[], r2: (number | null)[], final: number | null): Bracket =>
  ({
    isSeeded: true,
    matches: [
      match(1, 1, [1, null]),
      match(1, 2, [2, null]),
      match(1, r1[0], [3, 4]),
      match(1, r1[1], [5, 6]),
      match(2, r2[0]),
      match(2, r2[1]),
      match(3, final),
    ],
  }) as Bracket;

describe('overview status', () => {
  it('counts finished groups and finished bracket rounds', () => {
    const groups = [group('Group A', true, []), group('Group B', false, [])];
    const counts = stageCounts(tournament(), groups, bracket([3, 5], [1, null], null));

    expect(counts).toEqual({ currentTeams: 11, maxTeams: 16, groupsFinished: 1, groupsTotal: 2, roundsFinished: 1, roundsTotal: 3 });
  });

  it('has no rounds before the bracket is seeded', () => {
    const counts = stageCounts(tournament(), [], { isSeeded: false, matches: [] } as unknown as Bracket);
    expect(counts.roundsTotal).toBe(0);
  });

  it('makes one card per started group and bracket round, BYEs not counted as matches', () => {
    const cards = stageCards([group('Group A', true, [match(1, 1), match(1, 2)])], bracket([3, null], [null, null], null));

    expect(cards).toEqual([
      { name: 'Group A', finished: true, progress: '2 / 2 matches' },
      { name: 'Quarterfinals', finished: false, progress: '1 / 2 matches' },
      { name: 'Semifinals', finished: false, progress: '0 / 2 matches' },
      { name: 'Final', finished: false, progress: '0 / 1 match' },
    ]);
  });

  it('shows registration with the start action from the state flags', () => {
    const t = tournament();
    const card = statusCard(t, state({ canStartGroups: true }), stageCounts(t, [], null), []);

    expect(card).toEqual({
      label: 'Registration open',
      headline: '11 of 16 teams registered',
      text: 'Start the groups when everyone is in. Starting draws the groups and closes registration.',
      action: 'startGroups',
    });
  });

  it('has no action while groups are played, Start bracket when they are done', () => {
    const groups = [group('Group A', true, []), group('Group B', false, [])];
    const live = tournament({ status: TournamentStatus.GroupsInProgress, currentTeams: 16 });
    const liveCard = statusCard(live, state(), stageCounts(live, groups, null), []);
    expect(liveCard.headline).toBe('1 of 2 groups finished');
    expect(liveCard.action).toBeNull();

    const done = tournament({ status: TournamentStatus.GroupsCompleted });
    const doneCard = statusCard(done, state({ canStartBracket: true }), stageCounts(done, groups, null), []);
    expect(doneCard.headline).toBe('All 2 groups finished');
    expect(doneCard.action).toBe('startBracket');
  });

  it('names the champion, or the group winners', () => {
    const t = tournament({ status: TournamentStatus.Finished });
    const counts = stageCounts(t, [], null);
    const winner = (teamName: string) => ({ teamName, placement: 1 }) as TeamPlacement;

    expect(statusCard(t, state(), counts, [winner('Bravo')]).headline).toBe('Bravo won');
    expect(statusCard(t, state(), counts, [winner('Alpha'), winner('Echo')]).headline).toBe('Group winners: Alpha, Echo');
  });

  it('shows who cancelled and why', () => {
    const t = tournament({ status: TournamentStatus.Cancelled, cancelledBy: 'anna', cancellationReason: 'Venue closed' });
    const card = statusCard(t, state(), stageCounts(t, [], null), []);
    expect(card).toMatchObject({ label: 'Cancelled', headline: 'Cancelled by anna', text: 'Venue closed' });
  });

  it('shows counts under reached stage segments only', () => {
    const t = tournament({ status: TournamentStatus.GroupsInProgress, currentTeams: 16 });
    const counts = stageCounts(t, [group('Group A', true, []), group('Group B', false, [])], null);
    const labels = stageSteps(t.format, t.status).map((step) => stageCount(step, counts));

    expect(labels).toEqual(['16 / 16', '1 / 2', null, null]);
  });
});
