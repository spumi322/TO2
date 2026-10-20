import { CreateTournamentRequest } from '../../core/models';
import { Format, formatHasBracket, formatHasGroups } from '../../shared/enums';

// Pure calculations for the create form's live preview and client checks.
// The server (CreateTournamentValidator, pipelines) stays the source of truth.

export const MIN_TEAMS = 4;
export const MAX_TEAMS = 32;
export const MIN_GROUP_SIZE = 3;

export interface TournamentConfig {
  name: string;
  description: string;
  maxTeams: number;
  format: Format;
  numberOfGroups: number | null;
  advancingPerGroup: number | null;
}

export function nextPowerOfTwo(n: number): number {
  if (n <= 1) return 2;
  let p = 1;
  while (p < n) p <<= 1;
  return p;
}

// Teams split evenly; the first groups take the extra teams (same as the backend)
export function distributeTeams(maxTeams: number, numberOfGroups: number): number[] {
  if (numberOfGroups < 1) return [];
  const base = Math.floor(maxTeams / numberOfGroups);
  const extra = maxTeams % numberOfGroups;
  return Array.from({ length: numberOfGroups }, (_, i) => base + (i < extra ? 1 : 0));
}

export function roundRobinMatches(teams: number): number {
  return (teams * (teams - 1)) / 2;
}

export function bracketTeamCount(config: Pick<TournamentConfig, 'format' | 'maxTeams' | 'numberOfGroups' | 'advancingPerGroup'>): number {
  if (config.format === Format.BracketOnly) return config.maxTeams;
  if (config.format === Format.GroupsAndBracket && config.numberOfGroups && config.advancingPerGroup) {
    return config.numberOfGroups * config.advancingPerGroup;
  }
  return 0;
}

export function computeBracketSize(teamCount: number): number {
  return teamCount < 2 ? 0 : nextPowerOfTwo(teamCount);
}

export function computeByeCount(bracketSize: number, teamCount: number): number {
  return Math.max(0, bracketSize - teamCount);
}

/**
 * First-round pairs as [seed, seed] (1-based) in bracket order: seed i meets seed N+1-i,
 * and the top seeds are spread so they can only meet late. Seeds above the team count are BYEs.
 */
export function generateMatchups(bracketSize: number): [number, number][] {
  if (bracketSize < 2 || (bracketSize & (bracketSize - 1)) !== 0) return [];

  let order = [1, 2];
  while (order.length < bracketSize) {
    const size = order.length * 2;
    order = order.flatMap((seed) => [seed, size + 1 - seed]);
  }

  const matchups: [number, number][] = [];
  for (let i = 0; i < bracketSize; i += 2) matchups.push([order[i], order[i + 1]]);
  return matchups;
}

// Client copy of CreateTournamentValidator, for instant feedback; keys are form control names
export function validateConfig(config: TournamentConfig): Partial<Record<keyof TournamentConfig, string>> {
  const errors: Partial<Record<keyof TournamentConfig, string>> = {};
  const name = config.name.trim();

  if (name.length < 4 || name.length > 60) errors.name = 'Name must be 4-60 characters long.';
  if (config.description.length > 250) errors.description = 'Description cannot exceed 250 characters.';
  if (!Number.isInteger(config.maxTeams) || config.maxTeams < MIN_TEAMS || config.maxTeams > MAX_TEAMS) {
    errors.maxTeams = `Max teams must be between ${MIN_TEAMS} and ${MAX_TEAMS}.`;
  }

  if (formatHasGroups(config.format)) {
    const groups = config.numberOfGroups ?? 0;
    if (groups < 1) errors.numberOfGroups = 'At least 1 group.';
    else if (Math.floor(config.maxTeams / groups) < MIN_GROUP_SIZE) {
      errors.numberOfGroups = `Each group needs at least ${MIN_GROUP_SIZE} teams.`;
    }
  }

  if (config.format === Format.GroupsAndBracket && !errors.numberOfGroups) {
    const groups = config.numberOfGroups ?? 0;
    const advancing = config.advancingPerGroup ?? 0;
    const minGroupSize = Math.floor(config.maxTeams / groups);
    if (advancing < 1) errors.advancingPerGroup = 'At least 1 team must advance.';
    else if (groups === 1 && advancing < 2) errors.advancingPerGroup = 'With one group, at least 2 teams must advance.';
    else if (advancing >= minGroupSize) {
      errors.advancingPerGroup = 'At least one team per group must be eliminated.';
    }
  }

  return errors;
}

export function buildCreateRequest(config: TournamentConfig): CreateTournamentRequest {
  const description = config.description.trim();
  return {
    name: config.name.trim(),
    description: description || null,
    maxTeams: config.maxTeams,
    format: config.format,
    numberOfGroups: formatHasGroups(config.format) ? config.numberOfGroups : null,
    advancingPerGroup: config.format === Format.GroupsAndBracket ? config.advancingPerGroup : null,
  };
}

export interface ConfigPreview {
  groupSizes: number[];
  groupMatches: number;
  bracketTeams: number;
  bracketSize: number;
  byes: number;
  rounds: number;
  matchups: [number, number][];
}

export function previewConfig(config: TournamentConfig): ConfigPreview {
  const groupSizes = formatHasGroups(config.format) ? distributeTeams(config.maxTeams, config.numberOfGroups ?? 0) : [];
  const bracketTeams = formatHasBracket(config.format) ? bracketTeamCount(config) : 0;
  const bracketSize = computeBracketSize(bracketTeams);

  return {
    groupSizes,
    groupMatches: groupSizes.reduce((sum, size) => sum + roundRobinMatches(size), 0),
    bracketTeams,
    bracketSize,
    byes: computeByeCount(bracketSize, bracketTeams),
    rounds: bracketSize ? Math.log2(bracketSize) : 0,
    matchups: generateMatchups(bracketSize),
  };
}
