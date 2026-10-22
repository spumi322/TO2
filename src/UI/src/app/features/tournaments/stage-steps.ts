import { TournamentListItem } from '../../core/models';
import { Format, TournamentStatus } from '../../shared/enums';

export type StageKey = 'registration' | 'groups' | 'playoffs' | 'finished';

// ready = the stage before is done but this one has not started (groups done, playoffs not started)
export type StepState = 'done' | 'current' | 'ready' | 'todo';

export interface StageStep {
  key: StageKey;
  label: string;
  state: StepState;
}

const STAGES: Record<Format, StageKey[]> = {
  [Format.BracketOnly]: ['registration', 'playoffs', 'finished'],
  [Format.GroupsOnly]: ['registration', 'groups', 'finished'],
  [Format.GroupsAndBracket]: ['registration', 'groups', 'playoffs', 'finished'],
};

const LABELS: Record<StageKey, string> = {
  registration: 'Registration',
  groups: 'Groups',
  playoffs: 'Playoffs',
  finished: 'Finished',
};

// The stage each status is at (Finished and Cancelled are handled before)
const POSITION: Partial<Record<TournamentStatus, { key: StageKey; state: 'current' | 'ready' }>> = {
  [TournamentStatus.Setup]: { key: 'registration', state: 'current' },
  [TournamentStatus.SeedingGroups]: { key: 'groups', state: 'current' },
  [TournamentStatus.GroupsInProgress]: { key: 'groups', state: 'current' },
  [TournamentStatus.GroupsCompleted]: { key: 'playoffs', state: 'ready' },
  [TournamentStatus.SeedingBracket]: { key: 'playoffs', state: 'current' },
  [TournamentStatus.BracketInProgress]: { key: 'playoffs', state: 'current' },
};

// Stages of the format with how far the tournament got, from format and status only
export function stageSteps(format: Format, status: TournamentStatus): StageStep[] {
  const keys = STAGES[format];
  if (status === TournamentStatus.Cancelled) return [];
  if (status === TournamentStatus.Finished) {
    return keys.map((key) => ({ key, label: LABELS[key], state: 'done' }));
  }

  const position = POSITION[status]!;
  const index = keys.indexOf(position.key);
  return keys.map((key, i) => ({
    key,
    label: LABELS[key],
    state: i < index ? 'done' : i === index ? position.state : 'todo',
  }));
}

export type StageCounts = Pick<
  TournamentListItem,
  'currentTeams' | 'maxTeams' | 'groupsFinished' | 'groupsTotal' | 'roundsFinished' | 'roundsTotal'
>;

const percent = (done: number, total: number) => (total > 0 ? Math.min(100, (done / total) * 100) : 0);

// How full a segment is (0-100). Registration always shows registered / max teams, like the slots bar;
// the current Groups/Playoffs segment fills by finished groups / bracket rounds; other done stages are full
export function stageFill(step: StageStep, counts: StageCounts): number {
  if (step.state === 'todo' || step.state === 'ready') return 0;
  if (step.key === 'registration') return percent(counts.currentTeams, counts.maxTeams);
  if (step.state === 'done') return 100;
  if (step.key === 'groups') return percent(counts.groupsFinished, counts.groupsTotal);
  if (step.key === 'playoffs') return percent(counts.roundsFinished, counts.roundsTotal);
  return 100;
}
