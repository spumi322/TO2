import { Component, Input, OnInit } from '@angular/core';
import { Standing } from '../../../models/standing';
import { Match } from '../../../models/match';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit {
  @Input() brackets: Standing[] = [];
  rounds: { roundNumber: number, matches: Match[] }[] = [];

  ngOnInit() {
    this.generateBracket();
  }

  ngOnChanges() {
    this.generateBracket();
  }

  generateBracket(): void {
    if (this.brackets.length > 0 && this.brackets[0].matches) {
      const matches = this.brackets[0].matches;

      // Group matches by round
      const roundsMap = new Map<number, Match[]>();

      matches.forEach(match => {
        const round = match.round || 1;
        if (!roundsMap.has(round)) {
          roundsMap.set(round, []);
        }
        roundsMap.get(round)?.push(match);
      });

      // Convert to array and sort by round number
      this.rounds = Array.from(roundsMap.entries())
        .map(([roundNumber, matches]) => ({
          roundNumber,
          matches: matches.sort((a, b) => (a.seed || 0) - (b.seed || 0))
        }))
        .sort((a, b) => a.roundNumber - b.roundNumber);
    }
  }

  getTeamName(teamId: number): string {
    if (!this.brackets || this.brackets.length === 0) return 'TBD';

    const team = this.brackets[0].teams?.find(t => t.id === teamId);
    return team ? team.name : 'TBD';
  }

  getMatchWins(match: Match, teamId: number): number {
    if (!match.games) return 0;
    return match.games.filter(g => g.winnerId === teamId).length;
  }

  onMatchFinished(event: any) {
    // Refresh bracket when match finishes
    this.generateBracket();
  }
}
