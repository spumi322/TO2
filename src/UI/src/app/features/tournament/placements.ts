import { TeamPlacement } from '../../core/models';

export interface PlacementRow extends TeamPlacement {
  // "1st", or a range for shared ranks: "3rd-4th"
  rankLabel: string;
  // How far the team got: "Champion", "Final", "Semifinals", "Group A", ...
  reached: string;
}

export function ordinal(n: number): string {
  const tens = n % 100;
  if (tens >= 11 && tens <= 13) return `${n}th`;
  return `${n}${['th', 'st', 'nd', 'rd'][n % 10] ?? 'th'}`;
}

// Bracket round -> name, counted from the final (same names as the bracket view)
export function stageReached(eliminatedInRound: number, totalRounds: number): string {
  const fromFinal = totalRounds - eliminatedInRound;
  if (fromFinal === 0) return 'Final';
  if (fromFinal === 1) return 'Semifinals';
  if (fromFinal === 2) return 'Quarterfinals';
  return `Round of ${2 ** (fromFinal + 1)}`;
}

/**
 * Placements come from the server, already ranked; this only labels them.
 * Teams sharing a placement get a range ("5th-8th"). A group name means the placement is a group position.
 */
export function toPlacementRows(placements: TeamPlacement[], totalRounds: number): PlacementRow[] {
  const sharing = new Map<number, number>();
  for (const p of placements) sharing.set(p.placement, (sharing.get(p.placement) ?? 0) + 1);

  return placements.map((p) => {
    const count = sharing.get(p.placement) ?? 1;
    const rankLabel = count > 1 ? `${ordinal(p.placement)}-${ordinal(p.placement + count - 1)}` : ordinal(p.placement);

    let reached: string;
    if (p.groupName) reached = p.placement === 1 ? `${p.groupName} winner` : p.groupName;
    else if (p.placement === 1) reached = 'Champion';
    else if (p.eliminatedInRound > 0 && totalRounds > 0) reached = stageReached(p.eliminatedInRound, totalRounds);
    else reached = '';

    return { ...p, rankLabel, reached };
  });
}

// Top placement tiers for the podium (shared ranks stay together)
export function podiumTiers(placements: TeamPlacement[], tiers = 3): TeamPlacement[][] {
  const byPlacement = new Map<number, TeamPlacement[]>();
  for (const p of placements) byPlacement.set(p.placement, [...(byPlacement.get(p.placement) ?? []), p]);
  return [...byPlacement.entries()]
    .sort(([a], [b]) => a - b)
    .slice(0, tiers)
    .map(([, teams]) => teams);
}
