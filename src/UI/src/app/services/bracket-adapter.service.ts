import { Injectable } from '@angular/core';
import { Tournament } from '../models/tournament';
import { Match } from '../models/match';

@Injectable({
  providedIn: 'root'
})
export class BracketAdapterService {

  async transformToBracketsViewer(tournament: Tournament, matches: Match[]): Promise<any | null> {
    const bracketMatches = matches.filter(m => m.round != null);

    if (bracketMatches.length === 0 || !tournament.teams?.length) {
      return null;
    }

    try {
      // Map participants from tournament teams
      const participants = tournament.teams.map((team, index) => ({
        id: team.id,
        tournament_id: tournament.id,
        name: team.name
      }));

      // Create synthetic stage for the tournament
      const stage = {
        id: 1,
        tournament_id: tournament.id,
        name: 'Main Bracket',
        type: 'single_elimination',
        number: 1,
        settings: {}
      };

      // Create synthetic group (single elimination has one group)
      const group = {
        id: 1,
        stage_id: stage.id,
        number: 1
      };

      // Generate rounds from unique round numbers in matches
      const uniqueRounds = [...new Set(bracketMatches.map(m => m.round))].sort((a, b) => a - b);
      const rounds = uniqueRounds.map((roundNum, index) => ({
        id: index + 1,
        group_id: group.id,
        number: roundNum
      }));

      // Helper function to get round_id from round number
      const getRoundId = (roundNum: number): number => {
        const round = rounds.find(r => r.number === roundNum);
        return round ? round.id : 1;
      };

      // Helper function to determine match status
      const getMatchStatus = (match: Match): number => {
        if (match.winnerId) {
          return 4; // Completed
        }
        if (match.teamAId && match.teamBId) {
          return 2; // Ready (both teams present)
        }
        return 0; // Locked (waiting for teams)
      };

      // Map backend matches to brackets-viewer format
      const viewerMatches = bracketMatches.map(match => {
        // Use match.result scores (calculated from games, same as groups component)
        const scoreA = match.result?.teamAWins ?? 0;
        const scoreB = match.result?.teamBWins ?? 0;

        return {
          id: match.id,
          stage_id: stage.id,
          group_id: group.id,
          round_id: getRoundId(match.round),
          number: match.seed,
          status: getMatchStatus(match),
          opponent1: match.teamAId ? {
            id: match.teamAId,
            score: scoreA,
            result: match.winnerId === match.teamAId ? 'win' : (match.winnerId ? 'loss' : undefined)
          } : null,
          opponent2: match.teamBId ? {
            id: match.teamBId,
            score: scoreB,
            result: match.winnerId === match.teamBId ? 'win' : (match.winnerId ? 'loss' : undefined)
          } : null,
          child_count: match.bestOf || 1
        };
      });

      // Map match games to brackets-viewer format
      const matchGames: any[] = [];
      bracketMatches.forEach(match => {
        if (match.games && match.games.length > 0) {
          match.games.forEach((game, index) => {
            matchGames.push({
              id: game.id,
              match_id: match.id,
              number: index + 1,
              status: game.winnerId ? 4 : 2, // Completed if has winner, Ready otherwise
              opponent1: {
                id: game.teamAId,
                score: game.teamAScore
              },
              opponent2: {
                id: game.teamBId,
                score: game.teamBScore
              }
            });
          });
        }
      });

      return {
        stages: [stage],
        groups: [group],
        rounds: rounds,
        matches: viewerMatches,
        matchGames: matchGames,
        participants: participants
      };
    } catch (error) {
      console.error('Error transforming bracket data:', error);
      return null;
    }
  }

}
