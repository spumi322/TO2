import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Standing } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { Tournament } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { Match } from '../../../models/match';
import { MatchFinishedIds } from '../../../models/matchresult';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit {
  @Input() tournament!: Tournament;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();

  bracket$: Observable<Standing | null> = of(null);
  bracket: Standing | null = null;
  rounds: Match[][] = [];

  constructor(
    private standingService: StandingService,
    private matchService: MatchService
  ) { }

  ngOnInit(): void {
    this.refreshBracket();
  }

  refreshBracket(): void {
    if (this.tournament.id) {
      this.bracket$ = this.standingService.getBracketsByTournamentId(this.tournament.id).pipe(
        switchMap((brackets) => {
          if (!brackets || brackets.length === 0) {
            return of(null);
          }

          const bracket = brackets[0]; // Get first bracket

          return this.matchService.getMatchesByStandingId(bracket.id).pipe(
            map((matches) => {
              // Organize matches by rounds
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

    // Find the maximum round number
    const maxRound = Math.max(...matches.map(m => m.round || 1));

    // Initialize rounds array
    this.rounds = [];

    // Group matches by round
    for (let round = 1; round <= maxRound; round++) {
      const roundMatches = matches
        .filter(m => m.round === round)
        .sort((a, b) => (a.seed || 0) - (b.seed || 0));

      this.rounds.push(roundMatches);
    }
  }

  getRoundName(roundIndex: number): string {
    const matchesInRound = this.rounds[roundIndex]?.length || 0;

    if (matchesInRound === 1) return 'Finals';
    if (matchesInRound === 2) return 'Semi-Finals';
    if (matchesInRound === 4) return 'Quarter-Finals';

    return `Round ${roundIndex + 1}`;
  }

  getTeamName(teamId: number | null | undefined): string {
    if (!teamId) return 'TBD';

    const team = this.tournament.teams?.find(t => t.id === teamId);
    return team?.name || 'Unknown Team';
  }

  onMatchFinished(matchUpdate: MatchFinishedIds): void {
    this.matchFinished.emit(matchUpdate);

    // Refresh bracket after a short delay to allow backend to update
    setTimeout(() => {
      this.refreshBracket();
    }, 500);
  }
}
