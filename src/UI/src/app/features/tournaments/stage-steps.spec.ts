import { Format, TournamentStatus } from '../../shared/enums';
import { stageSteps } from './stage-steps';

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

  it('marks every stage done when finished, none when cancelled', () => {
    expect(states(Format.GroupsOnly, TournamentStatus.Finished)).toEqual(['registration:done', 'groups:done', 'finished:done']);
    expect(stageSteps(Format.BracketOnly, TournamentStatus.Cancelled)).toEqual([]);
  });
});
