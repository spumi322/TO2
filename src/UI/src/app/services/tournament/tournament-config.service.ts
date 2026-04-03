import { Injectable } from '@angular/core';
import { CreateTournamentRequest, Format } from '../../models/tournament';

@Injectable({ providedIn: 'root' })
export class TournamentConfigService {

  nextPowerOfTwo(n: number): number {
    if (n <= 1) return 2;
    let p = 1;
    while (p < n) p <<= 1;
    return p;
  }

  distributeTeams(maxTeams: number, numberOfGroups: number): number[] {
    const base = Math.floor(maxTeams / numberOfGroups);
    const extra = maxTeams % numberOfGroups;
    return Array.from({ length: numberOfGroups }, (_, i) => base + (i < extra ? 1 : 0));
  }

  computeBracketSize(format: Format, maxTeams: number, numberOfGroups?: number, advancingPerGroup?: number): number {
    if (format === Format.BracketOnly) {
      return this.nextPowerOfTwo(maxTeams);
    }
    if (format === Format.GroupsAndBracket && numberOfGroups && advancingPerGroup) {
      return this.nextPowerOfTwo(numberOfGroups * advancingPerGroup);
    }
    return 0;
  }

  computeByeCount(bracketSize: number, teamCount: number): number {
    return Math.max(0, bracketSize - teamCount);
  }

  // Returns first-round matchups as [seed1, seed2] pairs (1-indexed).
  // Seeds exceeding teamCount are BYE slots.
  generateMatchups(bracketSize: number): [number, number][] {
    if (bracketSize < 2 || (bracketSize & (bracketSize - 1)) !== 0) return [];

    const order = new Array<number>(bracketSize).fill(0);
    order[0] = 0;
    order[1] = bracketSize - 1;
    let filled = 2;
    const rounds = Math.log2(bracketSize);
    for (let r = 1; r < rounds; r++) {
      const step = bracketSize / Math.pow(2, r + 1);
      const cur = filled;
      for (let i = 0; i < cur; i += 2) {
        order[filled++] = order[i] + step;
        order[filled++] = order[i + 1] - step;
      }
    }

    const matchups: [number, number][] = [];
    for (let i = 0; i < bracketSize; i += 2) {
      matchups.push([order[i] + 1, order[i + 1] + 1]);
    }
    return matchups;
  }

  buildCreateRequest(form: {
    name: string;
    description: string;
    maxTeams: number;
    format: Format;
    numberOfGroups?: number | null;
    advancingPerGroup?: number | null;
  }): CreateTournamentRequest {
    return {
      name: form.name,
      description: form.description,
      maxTeams: form.maxTeams,
      format: form.format,
      numberOfGroups: form.numberOfGroups ?? undefined,
      advancingPerGroup: form.advancingPerGroup ?? undefined,
    };
  }
}
