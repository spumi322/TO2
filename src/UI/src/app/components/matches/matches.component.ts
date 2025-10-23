import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Match } from '../../models/match';
import { Team } from '../../models/team';
import { MatchService } from '../../services/match/match.service';
import { MatchFinishedIds, MatchResult } from '../../models/matchresult';
import { Game } from '../../models/game';

@Component({
  selector: 'app-matches',
  templateUrl: './matches.component.html',
  styleUrls: ['./matches.component.css'] 
})
export class MatchesComponent implements OnInit {
  @Input() matches: Match[] = [];
  @Input() teams: Team[] = [];
  @Input() isGroupFinished: boolean = false;
  @Input() tournamentId!: number;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();

  isUpdating: { [key: number]: boolean } = {};

  constructor(private matchService: MatchService) { }

  ngOnInit() {
    this.loadAllMatchScores();
  }

  getTeamName(teamId: number): string {
    const team = this.teams.find(t => t.id === teamId);
    return team ? team.name : 'TBD';
  }

  loadAllMatchScores(): void {
    this.matches.forEach(match => {
      match.result = match.result || { teamAId: match.teamAId, teamAWins: 0, teamBId: match.teamBId, teamBWins: 0 };
      this.matchService.getAllGamesByMatch(match.id).subscribe(games => {
        match.result = this.getMatchResults(games);
      });
    });
  }

  getMatchResults(games: Game[]): MatchResult {
    let teamAWins = 0;
    let teamBWins = 0;
    let teamAId = games[0]?.teamAId ?? 0;
    let teamBId = games[0]?.teamBId ?? 0;

    games.forEach(game => {
      if (game.winnerId === teamAId) {
        teamAWins++;
      } else if (game.winnerId === teamBId) {
        teamBWins++;
      }
    });

    return { teamAId, teamAWins, teamBId, teamBWins };
  }

  updateMatchScore(matchId: number, gameWinnerId: number): void {
    const match = this.matches.find(m => m.id === matchId);
    if (!match || match.winnerId) return;

    this.isUpdating[matchId] = true;

    this.matchService.getAllGamesByMatch(matchId).subscribe(games => {
      const gameToUpdate = games.find(game => !game.winnerId);
      if (gameToUpdate) {
        const gameResult = {
          gameId: gameToUpdate.id,
          winnerId: gameWinnerId,
          teamAScore: undefined,
          teamBScore: undefined,
          matchId: matchId,
          standingId: match.standingId,
          tournamentId: this.tournamentId
        };
        this.matchService.setGameResult(gameResult).subscribe(result => {
          // Check if match finished and update accordingly
          if (result.matchFinished && result.matchWinnerId && result.matchLoserId) {
            match.winnerId = result.matchWinnerId;
            match.loserId = result.matchLoserId;
            
            // Convert to MatchFinishedIds format for parent component
            const matchFinishedData: MatchFinishedIds = {
              winnerId: result.matchWinnerId,
              loserId: result.matchLoserId,
              allGroupsFinished: result.allGroupsFinished
            };
            this.matchFinished.emit(matchFinishedData);
          }
          
          // Refresh match scores regardless of match completion
          this.matchService.getAllGamesByMatch(matchId).subscribe(updatedGames => {
            match.result = this.getMatchResults(updatedGames);
            this.isUpdating[matchId] = false;
          });
        });
      } else {
        this.isUpdating[matchId] = false;
      }
    });
  }
}
