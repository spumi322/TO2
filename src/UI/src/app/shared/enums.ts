// Enums arrive from the API as numbers (no string enum converter on the backend)

export enum TournamentStatus {
  Setup = 1,
  SeedingGroups = 2,
  GroupsInProgress = 3,
  GroupsCompleted = 4,
  SeedingBracket = 5,
  BracketInProgress = 6,
  Finished = 7,
  Cancelled = 8,
}

export enum Format {
  BracketOnly = 1,
  GroupsAndBracket = 2,
  GroupsOnly = 3,
}

export enum TeamStatus {
  SignedUp = 0,
  Competing = 1,
  Advanced = 2,
  Eliminated = 3,
  Champion = 4,
}

export type ListSection = 'upcoming' | 'ongoing' | 'finished' | 'cancelled';

export interface StatusInfo {
  label: string;
  // CSS variable with the status color
  color: string;
  section: ListSection;
  // Matches are being played: pulsing dot
  live?: boolean;
  // Backend is drawing groups/bracket: spinner
  seeding?: boolean;
  // The next stage can start but has not: outlined chip
  outline?: boolean;
}

export const STATUS_INFO: Record<TournamentStatus, StatusInfo> = {
  [TournamentStatus.Setup]: { label: 'Registration', color: '--status-setup', section: 'upcoming' },
  [TournamentStatus.SeedingGroups]: { label: 'Drawing groups', color: '--status-seeding', section: 'ongoing', seeding: true },
  [TournamentStatus.GroupsInProgress]: { label: 'Groups live', color: '--status-groups', section: 'ongoing', live: true },
  [TournamentStatus.GroupsCompleted]: { label: 'Ready for playoffs', color: '--status-groups-done', section: 'ongoing', outline: true },
  [TournamentStatus.SeedingBracket]: { label: 'Seeding bracket', color: '--status-seeding', section: 'ongoing', seeding: true },
  [TournamentStatus.BracketInProgress]: { label: 'Playoffs live', color: '--status-bracket', section: 'ongoing', live: true },
  [TournamentStatus.Finished]: { label: 'Finished', color: '--status-finished', section: 'finished' },
  [TournamentStatus.Cancelled]: { label: 'Cancelled', color: '--status-cancelled', section: 'cancelled' },
};

export const FORMAT_LABELS: Record<Format, string> = {
  [Format.BracketOnly]: 'Bracket',
  [Format.GroupsAndBracket]: 'Groups + Bracket',
  [Format.GroupsOnly]: 'Groups',
};

export const FORMAT_DESCRIPTIONS: Record<Format, string> = {
  [Format.BracketOnly]: 'Single elimination, random seeding.',
  [Format.GroupsAndBracket]: 'Round robin groups, the best teams go to a playoff bracket.',
  [Format.GroupsOnly]: 'Round robin groups only, group position is the result.',
};

export const TEAM_STATUS_LABELS: Record<TeamStatus, string> = {
  [TeamStatus.SignedUp]: 'Signed up',
  [TeamStatus.Competing]: 'Competing',
  [TeamStatus.Advanced]: 'Advanced',
  [TeamStatus.Eliminated]: 'Eliminated',
  [TeamStatus.Champion]: 'Champion',
};

// Only for tab visibility; what is allowed comes from the tournament state flags
export function formatHasGroups(format: Format): boolean {
  return format === Format.GroupsOnly || format === Format.GroupsAndBracket;
}

export function formatHasBracket(format: Format): boolean {
  return format === Format.BracketOnly || format === Format.GroupsAndBracket;
}
