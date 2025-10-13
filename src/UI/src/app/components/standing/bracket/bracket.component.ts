import { Component, Input, OnInit } from '@angular/core';
import { Standing } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Observable, catchError, map, of, switchMap } from 'rxjs';
import { Tournament } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { Match } from '../../../models/match';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit {
  @Input() tournament!: Tournament;

  bracket$: Observable<Standing | null> = of(null);
  bracket: Standing | null = null;
  rounds: Match[][] = [];

  constructor(
    private standingService: StandingService,
    private matchService: MatchService
  ) { }

  ngOnInit(): void {
    this.loadBracket();
  }

  loadBracket(): void {
    if (this.tournament.id) {
      this.bracket$ = this.standingService.getBracketsByTournamentId(this.tournament.id).pipe(
        switchMap((brackets) => {
          if (!brackets || brackets.length === 0) {
            return of(null);
          }

          const bracket = brackets[0];

          return this.matchService.getMatchesByStandingId(bracket.id).pipe(
            map((matches) => {
              this.organizeBracketRounds(matches || []);
              return {
                ...bracket,
                matches: matches ?? []
              };
            }),
            catchError(() => of(bracket))
          );
        }),
        catchError(() => of(null))
      );

      this.bracket$.subscribe((bracket) => {
        this.bracket = bracket;
      });
    }
  }

  organizeBracketRounds(matches: Match[]): void {
    if (!matches || matches.length === 0) {
      this.rounds = [];
      return;
    }

    const maxRound = Math.max(...matches.map(m => m.round || 1));
    this.rounds = [];

    for (let round = 1; round <= maxRound; round++) {
      const roundMatches = matches
        .filter(m => m.round === round)
        .sort((a, b) => (a.seed || 0) - (b.seed || 0));

      this.rounds.push(roundMatches);
    }
  }

  getTeamName(teamId: number | null | undefined): string {
    if (!teamId) return 'TBD';
    const team = this.tournament.teams?.find(t => t.id === teamId);
    return team?.name || 'Unknown';
  }
}
