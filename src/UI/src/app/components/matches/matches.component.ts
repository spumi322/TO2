import { Component, Input, OnInit } from '@angular/core';
import { Match } from '../../models/match';
import { Team } from '../../models/team';
import { MatchService } from '../../services/match/match.service';
import { MatchResult } from '../../models/matchresult';
import { Game } from '../../models/game';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-matches',
  templateUrl: './matches.component.html',
  styleUrl: './matches.component.css'
})
export class MatchesComponent implements OnInit {
  @Input() matches: Match[] = [];
  @Input() teams: Team[] = [];
  private matchUpdateSub!: Subscription;


  constructor(private matchService: MatchService) { }

  ngOnInit() {
    this.loadAllMatchScores();

    this.matchUpdateSub = this.matchService.matchUpdated$.subscribe(() => {
      this.loadAllMatchScores();
    });
  }

  ngOnDestroy() {
    if (this.matchUpdateSub) {
      this.matchUpdateSub.unsubscribe();
    }
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

  updateMatchScore(matchId: number, gameWinnerId: number): void {
    const match = this.matches.find(m => m.id === matchId);
    if (match?.winnerId !== null || match?.loserId !== null) {
      return;
    }

    this.matchService.getAllGamesByMatch(matchId).subscribe(games => {
      const gameToUpdate = games.find(game => !game.winnerId || game.winnerId === undefined);
      if (gameToUpdate) {
        this.matchService.setGameResultOnlyWinner(gameToUpdate.id, gameWinnerId).subscribe(() => {
        });
      }
    });
  }
}
