import { Bracket, Group, StandingMatch, TeamPlacement, Tournament, TournamentState } from '../../../core/models';
import { Format, TournamentStatus } from '../../../shared/enums';
import { stageCount, stageSteps } from '../../tournaments/stage-steps';
import { currentRound, groupCards, roundCards, stageCounts, statusCard, topSlots } from './overview-status';

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
  ({ round, winnerId, teamAId: teams[0], teamBId: teams[1], teamAWins: 0, teamBWins: 0 }) as StandingMatch;

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

  it('makes one card per drawn group', () => {
    const drawn = group('Group A', true, [match(1, 1), match(1, 2)]);
    const notDrawn = { ...group('Group B', false, []), isSeeded: false };
    expect(groupCards([drawn, notDrawn])).toEqual([
      { name: 'Group A', finished: true, progress: '2 / 2 matches', group: drawn },
    ]);
  });

  it('makes one card per bracket round, BYEs not counted as matches', () => {
    expect(roundCards(bracket([3, null], [null, null], null))).toEqual([
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
      headline: '5 places left',
      text: 'Start the groups when everyone is in. Starting draws the groups and closes registration.',
      action: 'startGroups',
    });
  });

  it('has no action while groups are played, Start bracket when they are done', () => {
    const groups = [group('Group A', true, []), group('Group B', false, [])];
    const live = tournament({ status: TournamentStatus.GroupsInProgress, currentTeams: 16 });
    groups[0].matches = [match(1, 1), match(1, 2), match(1, null)];
    const liveCard = statusCard(live, state(), stageCounts(live, groups, null), [], groups);
    expect(liveCard).toMatchObject({ headline: 'Group stage', text: '2 of 3 matches played' });
    expect(liveCard.action).toBeNull();

    const done = tournament({ status: TournamentStatus.GroupsCompleted });
    const doneCard = statusCard(done, state({ canStartBracket: true }), stageCounts(done, groups, null), []);
    expect(doneCard.headline).toBe('Groups finished');
    expect(doneCard.action).toBe('startBracket');
  });

  it('names the round being played and its matches left', () => {
    const t = tournament({ status: TournamentStatus.BracketInProgress });
    const card = (b: Bracket) => statusCard(t, state(), stageCounts(t, [], b), [], [], b);

    // Round 1: 2 BYEs (not counted) and 2 played matches, one decided
    expect(card(bracket([3, null], [null, null], null))).toMatchObject({
      headline: 'Quarterfinals',
      text: '1 of 2 matches left in this round',
    });
    expect(card(bracket([3, 5], [1, 3], null))).toMatchObject({ headline: 'Final', text: 'Not played yet' });
  });

  it('counts the places left, Full when there are none', () => {
    const full = tournament({ currentTeams: 16 });
    expect(statusCard(full, state(), stageCounts(full, [], null), []).headline).toBe('Full');
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

  it('shows the first unfinished round match by match, without BYEs', () => {
    const names = new Map([[1, 'Alpha'], [2, 'Bravo'], [3, 'Charlie'], [4, 'Delta']]);
    // Round 1 done; semifinal 1 (teams 1, 2) won by team 1, semifinal 2 still waits for an opponent
    const b = bracket([3, 5], [1, null], null);
    b.matches[4] = { ...b.matches[4], teamAWins: 2, teamBWins: 1 };
    b.matches[5] = { ...b.matches[5], teamAId: 3, teamBId: null };
    const round = currentRound(b, names);

    expect(round?.name).toBe('Semifinals');
    expect(round?.matches.map((m) => [m.teamA, m.teamB, m.winner, `${m.teamAWins}-${m.teamBWins}`])).toEqual([
      ['Alpha', 'Bravo', 'A', '2-1'],
      ['Charlie', null, null, '0-0'],
    ]);
    expect(currentRound(bracket([3, 5], [1, 3], 1), names)).toBeNull();
  });

  describe('top placements', () => {
    const names = new Map([1, 2, 3, 4, 5, 6].map((id) => [id, `Team ${id}`]));
    const show = (slots: ReturnType<typeof topSlots>) => slots.map((slot) => [slot.rankLabel, slot.teamName, slot.groupName]);

    it('fills 3rd-4th with the semifinal losers before the final', () => {
      const t = tournament({ status: TournamentStatus.BracketInProgress });
      // Semifinal 1: team 1 beat team 2
      const slots = topSlots(t, [], bracket([3, 5], [1, null], null), [], names);

      expect(show(slots)).toEqual([
        ['1st', null, null],
        ['2nd', null, null],
        ['3rd-4th', 'Team 2', null],
        ['3rd-4th', null, null],
      ]);
    });

    it('shows the positions of finished groups in GroupsOnly', () => {
      const t = tournament({ format: Format.GroupsOnly, status: TournamentStatus.GroupsInProgress, currentTeams: 8 });
      const teams = [{ name: 'Alpha' }, { name: 'Bravo' }] as Group['teams'];
      const groups = [{ ...group('Group A', true, []), teams }, { ...group('Group B', false, []), teams }];

      expect(show(topSlots(t, groups, null, [], names))).toEqual([
        ['1st', 'Alpha', 'Group A'],
        ['1st', null, 'Group B'],
        ['2nd', 'Bravo', 'Group A'],
        ['2nd', null, 'Group B'],
      ]);
    });

    it('uses the final placements once finished', () => {
      const t = tournament({ status: TournamentStatus.Finished, format: Format.BracketOnly });
      const placement = (teamName: string, rank: number) => ({ teamName, placement: rank, groupName: null }) as TeamPlacement;
      const results = [placement('A', 1), placement('B', 2), placement('C', 3), placement('D', 3), placement('E', 5)];

      expect(show(topSlots(t, [], null, results, names))).toEqual([
        ['1st', 'A', null],
        ['2nd', 'B', null],
        ['3rd-4th', 'C', null],
        ['3rd-4th', 'D', null],
      ]);
    });
  });
});
