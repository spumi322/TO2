import { Component, Input, OnInit, OnChanges, AfterViewInit, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { Tournament, TournamentStateDTO, TournamentStatus } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { BracketAdapterService } from '../../../services/bracket-adapter.service';
import { Match } from '../../../models/match';
import { forkJoin } from 'rxjs';
import { MatchResult } from '../../../models/matchresult';
import { Game } from '../../../models/game';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit, OnChanges, AfterViewInit {
  @Input() tournament!: Tournament;
  @Input() standingId?: number;
  @Input() tournamentState?: TournamentStateDTO | null;
  @ViewChild('bracketContainer', { static: false }) bracketContainer?: ElementRef;

  matches: Match[] = [];
  isLoading = true;
  private viewInitialized = false;

  constructor(
    private matchService: MatchService,
    private bracketAdapter: BracketAdapterService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // ngOnInit fires when tab content is initialized (thanks to matTabContent lazy loading)
    // By this point, the element is in the DOM and visible
    this.loadMatches();
  }

  ngOnChanges() {
    if (this.viewInitialized) {
      this.loadMatches();
    }
  }

  ngAfterViewInit() {
    this.viewInitialized = true;
    if (this.matches.length > 0) {
      this.renderBracket();
    }
  }

  loadMatches() {
    if (!this.standingId || !this.tournament) {
      this.isLoading = false;
      return;
    }

    this.isLoading = true;

    this.matchService.getMatchesByStandingId(this.standingId).subscribe({
      next: (matches) => {
        this.matches = matches;
        this.loadAllMatchScores();
      },
      error: (err) => {
        console.error('Error loading matches:', err);
        this.isLoading = false;
      }
    });
  }

  loadAllMatchScores(): void {
    if (this.matches.length === 0) {
      this.isLoading = false;
      return;
    }

    const gameRequests = this.matches.map(match =>
      this.matchService.getAllGamesByMatch(match.id)
    );

    forkJoin(gameRequests).subscribe({
      next: (allGames) => {
        this.matches.forEach((match, index) => {
          match.games = allGames[index];
          match.result = this.getMatchResults(allGames[index]);
        });
        this.isLoading = false;

        if (this.viewInitialized) {
          this.renderBracket();
        }
      },
      error: (err) => {
        console.error('Error loading match games:', err);
        this.isLoading = false;
      }
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

  async renderBracket() {
    // Only render when bracket is actually in progress or finished (use tournament state)
    if (!this.tournamentState ||
        (this.tournamentState.currentStatus !== TournamentStatus.BracketInProgress &&
         this.tournamentState.currentStatus !== TournamentStatus.Finished)) {
      return;
    }

    if (!(window as any).bracketsViewer || !this.tournament || !this.matches?.length || !this.bracketContainer?.nativeElement || !this.standingId) {
      return;
    }

    const uniqueSelector = `#bracket-viewer-${this.standingId}`;

    this.cdr.detectChanges();
    await new Promise(resolve => setTimeout(resolve, 50));

    await this.performRender(uniqueSelector);
  }

  private async performRender(selector: string): Promise<void> {
    const data = await this.bracketAdapter.transformToBracketsViewer(
      this.tournament!,
      this.matches
    );

    if (!data) {
      return;
    }

    const viewerData = {
      stages: data.stages,
      groups: data.groups,
      rounds: data.rounds,
      matches: data.matches,
      matchGames: data.matchGames || [],
      participants: data.participants
    };

    try {
      await (window as any).bracketsViewer.render(viewerData, {
        selector: selector,
        clear: true
      });
    } catch (error) {
      console.error('Error rendering bracket:', error);
    }
  }
}
