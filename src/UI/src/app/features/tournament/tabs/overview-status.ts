import { Bracket, Group, StandingMatch, TeamPlacement, Tournament, TournamentState } from '../../../core/models';
import { Format, TournamentStatus } from '../../../shared/enums';
import { StageCounts } from '../../tournaments/stage-steps';
import { roundName } from '../bracket-view/bracket-adapter';

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
export function groupCards(groups: Group[]): StageCard[] {
  return groups
    .filter((group) => group.isSeeded)
    .map((group) => ({ name: group.name, finished: group.isFinished, progress: matchProgress(group.matches) }));
}

// Bracket rounds once the bracket is seeded
export function roundCards(bracket: Bracket | null): StageCard[] {
  const rounds = bracketRounds(bracket);
  return rounds.map((matches, index) => ({
    name: roundName(1 / 2 ** (rounds.length - 1 - index)),
    finished: allWon(matches),
    progress: matchProgress(matches),
  }));
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

// Text per status; the action only from the state flags (no status rules here)
export function statusCard(
  tournament: Tournament,
  state: TournamentState,
  counts: StageCounts,
  champions: TeamPlacement[],
): StatusCard {
  const action: StatusAction | null = state.canStartGroups ? 'startGroups' : state.canStartBracket ? 'startBracket' : null;
  const advancing = tournament.advancingPerGroup;
  const card = (label: string, headline: string, text: string): StatusCard => ({ label, headline, text, action });

  switch (tournament.status) {
    case TournamentStatus.Setup:
      return card('Registration open', `${counts.currentTeams} of ${counts.maxTeams} teams registered`, startText(tournament));
    case TournamentStatus.SeedingGroups:
      return card('Drawing groups', 'Drawing the groups', 'The teams are being drawn into the groups.');
    case TournamentStatus.GroupsInProgress:
      return card(
        'Groups live',
        `${counts.groupsFinished} of ${counts.groupsTotal} groups finished`,
        tournament.format === Format.GroupsOnly
          ? 'When every group is finished, the group positions are the final result.'
          : `When every group is finished, the top ${advancing} of each group go to the bracket.`,
      );
    case TournamentStatus.GroupsCompleted:
      return card(
        'Ready for playoffs',
        `All ${counts.groupsTotal} groups finished`,
        `The top ${advancing} of each group go to the bracket. Group winners meet runners-up in round 1.`,
      );
    case TournamentStatus.SeedingBracket:
      return card('Seeding bracket', 'Seeding the bracket', 'The teams are being placed in the bracket.');
    case TournamentStatus.BracketInProgress:
      return card(
        'Playoffs live',
        `${counts.roundsFinished} of ${counts.roundsTotal} rounds finished`,
        'The winner of the final wins the tournament.',
      );
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
