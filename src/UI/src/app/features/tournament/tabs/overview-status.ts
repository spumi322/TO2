import { Bracket, Group, StandingMatch, TeamPlacement, Tournament, TournamentState } from '../../../core/models';
import { Format, TournamentStatus } from '../../../shared/enums';
import { StageCounts } from '../../tournaments/stage-steps';
import { roundName } from '../bracket-view/bracket-adapter';
import { ordinal, toPlacementRows, topPlacements } from '../placements';

export type StatusAction = 'startGroups' | 'startBracket';

// The Overview's top card: where the tournament is and the one thing to do next
export interface StatusCard {
  label: string;
  headline: string;
  text: string;
  action: StatusAction | null;
}

// One card per group or bracket round in the Overview's left panel
export interface StageCard {
  name: string;
  finished: boolean;
  // "4 / 6 matches"
  progress: string;
}

// A group's card also shows its table
export interface GroupCard extends StageCard {
  group: Group;
}

// One match of the round being played; team null = not known yet
export interface MatchLine {
  id: number;
  teamA: string | null;
  teamB: string | null;
  teamAWins: number;
  teamBWins: number;
  winner: 'A' | 'B' | null;
}

// The first bracket round that still has matches to play, shown match by match on the Overview
export interface CurrentRound {
  name: string;
  matches: MatchLine[];
}

// One row of the Overview's top placements; teamName null = not decided yet
export interface TopSlot {
  rankLabel: string;
  teamName: string | null;
  groupName: string | null;
  winner: boolean;
}

// A round 1 match with an empty slot is a BYE: it has its winner from the start and is never played
const isBye = (match: StandingMatch) => match.round === 1 && (!match.teamAId || !match.teamBId);

// Bracket matches by round, in round order
function bracketRounds(bracket: Bracket | null): StandingMatch[][] {
  if (!bracket?.isSeeded) return [];
  const byRound = new Map<number, StandingMatch[]>();
  for (const match of bracket.matches) {
    const round = match.round ?? 0;
    byRound.set(round, [...(byRound.get(round) ?? []), match]);
  }
  return [...byRound.entries()].sort(([a], [b]) => a - b).map(([, matches]) => matches);
}

const allWon = (matches: StandingMatch[]) => matches.every((match) => match.winnerId !== null);

function matchProgress(matches: StandingMatch[]): string {
  const played = matches.filter((match) => !isBye(match));
  const won = played.filter((match) => match.winnerId !== null).length;
  return `${won} / ${played.length} ${played.length === 1 ? 'match' : 'matches'}`;
}

// Same counts as the tournament list's stage track (finished groups, finished bracket rounds)
export function stageCounts(tournament: Tournament, groups: Group[], bracket: Bracket | null): StageCounts {
  const rounds = bracketRounds(bracket);
  return {
    currentTeams: tournament.currentTeams,
    maxTeams: tournament.maxTeams,
    groupsFinished: groups.filter((group) => group.isFinished).length,
    groupsTotal: groups.length,
    roundsFinished: rounds.filter(allWon).length,
    roundsTotal: rounds.length,
  };
}

// Groups that are drawn
export function groupCards(groups: Group[]): GroupCard[] {
  return groups
    .filter((group) => group.isSeeded)
    .map((group) => ({ name: group.name, finished: group.isFinished, progress: matchProgress(group.matches), group }));
}

const bracketRoundName = (index: number, roundCount: number) => roundName(1 / 2 ** (roundCount - 1 - index));

// Bracket rounds once the bracket is seeded
export function roundCards(bracket: Bracket | null): StageCard[] {
  const rounds = bracketRounds(bracket);
  return rounds.map((matches, index) => ({
    name: bracketRoundName(index, rounds.length),
    finished: allWon(matches),
    progress: matchProgress(matches),
  }));
}

// null when the bracket is not seeded or every round is finished; BYEs are left out
export function currentRound(bracket: Bracket | null, teamNames: Map<number, string>): CurrentRound | null {
  const rounds = bracketRounds(bracket);
  const index = rounds.findIndex((matches) => !allWon(matches));
  if (index < 0) return null;

  const name = (id: number | null) => (id ? (teamNames.get(id) ?? null) : null);
  return {
    name: bracketRoundName(index, rounds.length),
    matches: rounds[index]
      .filter((match) => !isBye(match))
      .map((match) => ({
        id: match.id,
        teamA: name(match.teamAId),
        teamB: name(match.teamBId),
        teamAWins: match.teamAWins,
        teamBWins: match.teamBWins,
        winner: !match.winnerId ? null : match.winnerId === match.teamAId ? 'A' : 'B',
      })),
  };
}

function startText(tournament: Tournament): string {
  if (tournament.currentTeams < 2) return 'At least 2 teams are needed to start.';
  return tournament.format === Format.BracketOnly
    ? 'Start the bracket when everyone is in. Starting seeds the bracket and closes registration.'
    : 'Start the groups when everyone is in. Starting draws the groups and closes registration.';
}

function finishedHeadline(champions: TeamPlacement[]): string {
  if (champions.length === 1) return `${champions[0].teamName} won`;
  if (champions.length > 1) return `Group winners: ${champions.map((team) => team.teamName).join(', ')}`;
  return 'Tournament finished';
}

const plural = (n: number, word: string) => `${n} ${n === 1 ? word : `${word}es`}`;

// "18 of 40 matches played" over all groups
function groupMatchesText(groups: Group[]): string {
  const matches = groups.flatMap((group) => group.matches);
  const played = matches.filter((match) => match.winnerId !== null).length;
  return `${played} of ${plural(matches.length, 'match')} played`;
}

// The round being played and how many of its matches are left (BYEs not counted)
function roundStatus(bracket: Bracket | null): { name: string; text: string } {
  const rounds = bracketRounds(bracket);
  const index = rounds.findIndex((matches) => !allWon(matches));
  if (index < 0) return { name: 'Playoffs', text: '' };
  const played = rounds[index].filter((match) => !isBye(match));
  const left = played.filter((match) => match.winnerId === null).length;
  return {
    name: bracketRoundName(index, rounds.length),
    text: played.length === 1 ? 'Not played yet' : `${left} of ${plural(played.length, 'match')} left in this round`,
  };
}

// Headline = what is happening now, text = the detail the stage track does not show; the action only from the state flags
export function statusCard(
  tournament: Tournament,
  state: TournamentState,
  counts: StageCounts,
  champions: TeamPlacement[],
  groups: Group[] = [],
  bracket: Bracket | null = null,
): StatusCard {
  const action: StatusAction | null = state.canStartGroups ? 'startGroups' : state.canStartBracket ? 'startBracket' : null;
  const card = (label: string, headline: string, text: string): StatusCard => ({ label, headline, text, action });

  switch (tournament.status) {
    case TournamentStatus.Setup: {
      const free = counts.maxTeams - counts.currentTeams;
      return card('Registration open', free > 0 ? `${free} ${free === 1 ? 'place' : 'places'} left` : 'Full', startText(tournament));
    }
    case TournamentStatus.SeedingGroups:
      return card('Drawing groups', 'Drawing the groups', 'The teams are being drawn into the groups.');
    case TournamentStatus.GroupsInProgress:
      return card('Groups live', 'Group stage', groupMatchesText(groups));
    case TournamentStatus.GroupsCompleted:
      return card(
        'Ready for playoffs',
        'Groups finished',
        `The top ${tournament.advancingPerGroup} of each group go to the bracket. Group winners meet runners-up in round 1.`,
      );
    case TournamentStatus.SeedingBracket:
      return card('Seeding bracket', 'Seeding the bracket', 'The teams are being placed in the bracket.');
    case TournamentStatus.BracketInProgress: {
      const round = roundStatus(bracket);
      return card('Playoffs live', round.name, round.text);
    }
    case TournamentStatus.Finished:
      return card('Finished', finishedHeadline(champions), 'The final placements are in Results.');
    case TournamentStatus.Cancelled:
      return card(
        'Cancelled',
        tournament.cancelledBy ? `Cancelled by ${tournament.cancelledBy}` : 'Cancelled',
        tournament.cancellationReason ?? 'No reason given.',
      );
  }
}

// The other team of a decided match with two teams
function loserName(match: StandingMatch, teamNames: Map<number, string>): string | null {
  if (!match.winnerId || !match.teamAId || !match.teamBId) return null;
  return teamNames.get(match.winnerId === match.teamAId ? match.teamBId : match.teamAId) ?? null;
}

// GroupsOnly: the first positions of each group (whole tiers until 4 teams), filled when a group is finished
function groupSlots(tournament: Tournament, groups: Group[]): TopSlot[] {
  if (!groups.length) return [];
  const teamCount = tournament.status === TournamentStatus.Setup ? tournament.maxTeams : tournament.currentTeams;
  const tiers = Math.min(Math.ceil(4 / groups.length), Math.max(1, Math.floor(teamCount / groups.length)));
  const slots: TopSlot[] = [];
  for (let position = 1; position <= tiers; position++) {
    for (const group of groups) {
      if (group.isSeeded && group.teams.length < position) continue;
      const team = group.isFinished ? group.teams[position - 1] : undefined;
      slots.push({
        rankLabel: ordinal(position),
        teamName: team?.name ?? null,
        groupName: groups.length > 1 ? group.name : null,
        winner: position === 1 && !!team,
      });
    }
  }
  return slots;
}

// Bracket formats: 1st, 2nd, 3rd-4th; the semifinal losers are known before the final is played
function bracketSlots(tournament: Tournament, bracket: Bracket | null, teamNames: Map<number, string>): TopSlot[] {
  const rounds = bracketRounds(bracket);
  const size = rounds.length
    ? new Set(rounds[0].flatMap((match) => [match.teamAId, match.teamBId]).filter((id) => !!id)).size
    : tournament.format === Format.BracketOnly
      ? tournament.status === TournamentStatus.Setup ? tournament.maxTeams : tournament.currentTeams
      : (tournament.advancingPerGroup ?? 0) * tournament.numberOfGroups;

  const slot = (rankLabel: string, teamName: string | null = null): TopSlot => ({ rankLabel, teamName, groupName: null, winner: false });
  const slots = [slot('1st'), slot('2nd')].slice(0, Math.max(size, 2));
  const thirds = Math.min(2, size - 2);
  if (thirds > 0) {
    const semifinals = rounds.length >= 2 ? rounds[rounds.length - 2] : [];
    const losers = semifinals.map((match) => loserName(match, teamNames)).filter((name) => name !== null);
    for (let i = 0; i < thirds; i++) slots.push(slot(thirds === 2 ? '3rd-4th' : '3rd', losers[i] ?? null));
  }
  return slots;
}

// Always shown: the server's final placements once finished, before that what the groups and bracket already decided
export function topSlots(
  tournament: Tournament,
  groups: Group[],
  bracket: Bracket | null,
  results: TeamPlacement[],
  teamNames: Map<number, string>,
): TopSlot[] {
  const groupsOnly = tournament.format === Format.GroupsOnly;
  if (tournament.status === TournamentStatus.Finished && results.length) {
    return topPlacements(toPlacementRows(results, 0, groupsOnly)).map((row) => ({
      rankLabel: row.rankLabel,
      teamName: row.teamName,
      groupName: row.groupName,
      winner: row.placement === 1,
    }));
  }
  return groupsOnly ? groupSlots(tournament, groups) : bracketSlots(tournament, bracket, teamNames);
}
