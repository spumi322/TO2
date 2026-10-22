import { Format, TournamentStatus } from '../../shared/enums';
import { StageCounts, stageFill, stageSteps } from './stage-steps';

const states = (format: Format, status: TournamentStatus) => stageSteps(format, status).map((s) => `${s.key}:${s.state}`);

describe('stage steps', () => {
  it('has the stages of the format', () => {
    expect(stageSteps(Format.BracketOnly, TournamentStatus.Setup).map((s) => s.label)).toEqual(['Registration', 'Playoffs', 'Finished']);
    expect(stageSteps(Format.GroupsOnly, TournamentStatus.Setup).map((s) => s.label)).toEqual(['Registration', 'Groups', 'Finished']);
    expect(stageSteps(Format.GroupsAndBracket, TournamentStatus.Setup).map((s) => s.label)).toEqual([
      'Registration',
      'Groups',
      'Playoffs',
      'Finished',
    ]);
  });

  it('marks earlier stages done and the current one', () => {
    expect(states(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress)).toEqual([
      'registration:done',
      'groups:current',
      'playoffs:todo',
      'finished:todo',
    ]);
    expect(states(Format.BracketOnly, TournamentStatus.BracketInProgress)).toEqual(['registration:done', 'playoffs:current', 'finished:todo']);
  });

  it('shows playoffs as ready when groups are done', () => {
    expect(states(Format.GroupsAndBracket, TournamentStatus.GroupsCompleted)).toEqual([
      'registration:done',
      'groups:done',
      'playoffs:ready',
      'finished:todo',
    ]);
  });

  it('counts seeding as the stage being drawn', () => {
    expect(states(Format.GroupsAndBracket, TournamentStatus.SeedingBracket)[2]).toBe('playoffs:current');
    expect(states(Format.GroupsOnly, TournamentStatus.SeedingGroups)[1]).toBe('groups:current');
  });

  it('marks Finished current when finished, no stages when cancelled', () => {
    expect(states(Format.GroupsOnly, TournamentStatus.Finished)).toEqual(['registration:done', 'groups:done', 'finished:current']);
    expect(stageSteps(Format.BracketOnly, TournamentStatus.Cancelled)).toEqual([]);
  });

  describe('fill', () => {
    const counts: StageCounts = { currentTeams: 6, maxTeams: 8, groupsFinished: 1, groupsTotal: 4, roundsFinished: 2, roundsTotal: 4 };
    const fills = (format: Format, status: TournamentStatus) => stageSteps(format, status).map((s) => stageFill(s, counts));

    it('fills registration by registered / max teams', () => {
      expect(fills(Format.BracketOnly, TournamentStatus.Setup)).toEqual([75, 0, 0]);
    });

    it('fills the current groups segment by finished groups', () => {
      expect(fills(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress)).toEqual([75, 25, 0, 0]);
    });

    it('fills the current playoffs segment by finished rounds, earlier stages full', () => {
      expect(fills(Format.GroupsAndBracket, TournamentStatus.BracketInProgress)).toEqual([75, 100, 50, 0]);
    });

    it('leaves playoffs empty while ready, and a stage without standings empty', () => {
      expect(fills(Format.GroupsAndBracket, TournamentStatus.GroupsCompleted)).toEqual([75, 100, 0, 0]);
      const none = { ...counts, roundsFinished: 0, roundsTotal: 0 };
      expect(stageSteps(Format.BracketOnly, TournamentStatus.SeedingBracket).map((s) => stageFill(s, none))).toEqual([75, 0, 0]);
    });
  });
});
