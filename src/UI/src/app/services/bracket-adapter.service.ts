import { Injectable } from '@angular/core';
import { Tournament } from '../models/tournament';
import { Match } from '../models/match';

@Injectable({
  providedIn: 'root'
})
export class BracketAdapterService {

  /**
   * Transform TO2 data to brackets-viewer format
   *
   * IMPORTANT: We must provide the COMPLETE bracket structure (all rounds, all match slots)
   * even if opponents are TBD. The library renders based on the structure we provide.
   *
   * If we only give Round 1 matches → library only renders Round 1
   * We want full bracket → we must provide all rounds with empty slots
   *
   * @param tournament - TO2 Tournament with teams
   * @param matches - TO2 Match array (with Round and Seed)
   * @returns Complete data structure for brackets-viewer.js or null
   */
  transformToBracketsViewer(tournament: Tournament, matches: Match[]) {
    console.log('Raw matches from backend:', matches);
    console.log('Matches with round data:', matches.map(m => ({ id: m.id, round: m.round, seed: m.seed, teamA: m.teamAId, teamB: m.teamBId })));

    // Filter only bracket matches (those with Round set)
    const bracketMatches = matches.filter(m => m.round != null);
    console.log(`Filtered bracket matches: ${bracketMatches.length} out of ${matches.length} total`);

    if (bracketMatches.length === 0) {
      console.warn('No bracket matches found');
      return null;
    }

    // Calculate total rounds needed based on first round match count
    // If Round 1 has 4 matches (8 teams), we need 3 rounds total (4→2→1)
    const firstRoundMatches = bracketMatches.filter(m => m.round === 1);
    console.log(`First round matches:`, firstRoundMatches);

    // Calculate team count: each first-round match has 2 teams
    const teamCount = firstRoundMatches.length * 2;
    const totalRounds = Math.ceil(Math.log2(firstRoundMatches.length)) + 1;

    console.log(`First round has ${firstRoundMatches.length} matches (${teamCount} teams), creating ${totalRounds} total rounds`);

    // 1. Create synthetic stage (library requires this)
    const stage = [{
      id: 1,
      tournament_id: tournament.id,
      name: 'Main Bracket',
      type: 'single_elimination',
      number: 1,
      settings: { size: teamCount } // CRITICAL: Library needs to know bracket size!
    }];

    // 2. Create synthetic group (library requires this)
    const group = [{
      id: 1,
      stage_id: 1,
      number: 1
    }];

    // 3. Define all rounds upfront
    // Library needs to know: "There are 3 rounds total"
    const rounds = [];
    for (let i = 1; i <= totalRounds; i++) {
      rounds.push({
        id: i,
        stage_id: 1,
        group_id: 1,
        number: i
      });
    }

    // 4. Create match data structure for library
    // We provide ALL match slots (library decides how to render them)
    const viewerMatches = [];
    let matchIdCounter = 1;

    for (let roundNum = 1; roundNum <= totalRounds; roundNum++) {
      const round = rounds.find(r => r.number === roundNum)!;
      const matchesInThisRound = Math.pow(2, totalRounds - roundNum);

      for (let i = 0; i < matchesInThisRound; i++) {
        // Look up actual match data (only exists for Round 1 initially)
        const actualMatch = bracketMatches.find(
          m => m.round === roundNum && m.seed === i + 1
        );

        // Check if match has actual teams (not TBD with id=0)
        const hasTeams = actualMatch && actualMatch.teamAId > 0 && actualMatch.teamBId > 0;

        // Tell library about this match slot
        // Status: 0=Locked, 1=Waiting, 2=Ready, 3=Running, 4=Completed, 5=Archived
        viewerMatches.push({
          id: matchIdCounter++,
          stage_id: 1,
          group_id: 1,
          round_id: round.id,
          number: i + 1,
          child_count: 0,
          status: hasTeams ? 2 : 0, // 2=Ready (both teams set), 0=Locked (TBD)
          opponent1: hasTeams ? {
            id: actualMatch!.teamAId,
            score: 0
          } : null, // null = TBD slot
          opponent2: hasTeams ? {
            id: actualMatch!.teamBId,
            score: 0
          } : null
        });
      }
    }

    // 5. Transform participants (teams)
    const participants = tournament.teams?.map(team => ({
      id: team.id,
      tournament_id: tournament.id,
      name: team.name
    })) || [];

    const result = {
      stages: stage,
      groups: group,
      rounds: rounds,
      matches: viewerMatches,
      matchGames: [],
      participants: participants
    };

    console.log('Bracket structure prepared for library:', {
      stages: stage.length,
      groups: group.length,
      rounds: rounds.length,
      matches: viewerMatches.length,
      participants: participants.length
    });
    console.log('Stage with settings:', stage[0]);
    console.log('Settings object:', stage[0].settings);
    console.log('Stage data:', JSON.stringify(stage, null, 2));
    console.log('Full data being passed to library:', JSON.stringify(result, null, 2));

    // Return complete structure - library will render it
    return result;
  }

  /**
   * Generate round names (Final, Semi-finals, etc.)
   */
  private getRoundName(roundNumber: number, totalRounds: number): string {
    const fromEnd = totalRounds - roundNumber + 1;

    switch(fromEnd) {
      case 1: return 'Final';
      case 2: return 'Semi-finals';
      case 3: return 'Quarter-finals';
      case 4: return 'Round of 16';
      case 5: return 'Round of 32';
      default: return `Round ${roundNumber}`;
    }
  }
}
