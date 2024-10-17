import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Match } from '../../models/match';
import { Team } from '../../models/team';
import { MatchService } from '../../services/match/match.service';
import { MatchFinishedIds, MatchResult } from '../../models/matchresult';
import { Game } from '../../models/game';

@Component({
  selector: 'app-matches',
  templateUrl: './matches.component.html',
  styleUrl: './matches.component.css'
})
export class MatchesComponent implements OnInit {
  @Input() matches: Match[] = [];
  @Input() teams: Team[] = [];
  @Input() standingId!: number;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();

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
      match.result = { teamAId: match.teamAId, teamAWins: 0, teamBId: match.teamBId, teamBWins: 0 };
      this.matchService.getAllGamesByMatch(match.id).subscribe(games => {
        match.result = this.getMatchResults(games);
      });
    });
  }

  getMatchResults(games: Game[]): MatchResult {
    let teamAWins = 0;
    let teamBWins = 0;
    let teamAId = games[0]?.teamAId ?? '0';
    let teamBId = games[0]?.teamBId ?? '0';

    games.forEach(game => {
      if (game.winnerId === teamAId) {
        teamAWins++;
      } else if (game.winnerId === teamBId) {
        teamBWins++;
      }
    });

    return { teamAId, teamAWins, teamBId, teamBWins };
  }

  updateMatchScore(matchId: number, gameWinnerId: number, teamAScore?: number, teamBScore?: number): void {
    const match = this.matches.find(m => m.id === matchId);
    if (!match) {
      return;
    }

    const gameResult = { winnerId: gameWinnerId, teamAScore, teamBScore };

    this.matchService.getAllGamesByMatch(matchId).subscribe(games => {
      const gameToUpdate = games.find(game => !game.winnerId);
      if (gameToUpdate) {
        this.matchService.setGameResult(this.standingId, gameToUpdate.id, gameResult).subscribe((result: MatchFinishedIds | null) => {
          if (result) {
            match.winnerId = result.winnerId;
            match.loserId = result.loserId;
            this.matchFinished.emit(result);
          }
          this.matchService.getAllGamesByMatch(matchId).subscribe(updatedGames => {
            match.result = this.getMatchResults(updatedGames);
          });
        });
      }
    });
  }
}
