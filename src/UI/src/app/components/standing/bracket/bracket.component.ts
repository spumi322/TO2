import { Component, Input, Output, EventEmitter, OnInit, OnChanges, AfterViewInit, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { Tournament, TournamentStateDTO, TournamentStatus } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { StandingService } from '../../../services/standing/standing.service';
import { BracketAdapterService } from '../../../services/bracket-adapter.service';
import { Match } from '../../../models/match';
import { Team } from '../../../models/team';
import { MatchResult, MatchFinishedIds } from '../../../models/matchresult';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit, OnChanges, AfterViewInit {
  @Input() tournament!: Tournament;
  @Input() standingId?: number;
  @Input() tournamentState?: TournamentStateDTO | null;
  @Input() teams: Team[] = [];
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();
  @ViewChild('bracketContainer', { static: false }) bracketContainer?: ElementRef;

  matches: Match[] = [];
  isLoading = true;
  isUpdating: { [key: number]: boolean } = {};
  private viewInitialized = false;

  constructor(
    private standingService: StandingService,
    private matchService: MatchService,
    private bracketAdapter: BracketAdapterService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // ngOnInit fires when tab content is initialized (thanks to matTabContent lazy loading)
    // By this point, the element is in the DOM and visible
    this.loadBracket();
  }

  ngOnChanges() {
    if (this.viewInitialized) {
      this.loadBracket();
    }
  }

  ngAfterViewInit() {
    this.viewInitialized = true;
    if (this.matches.length > 0) {
      this.renderBracket();
    }
  }

  loadBracket(resetUpdatingFlag: number | null = null) {
    if (!this.tournament?.id) {
      this.isLoading = false;
      return;
    }

    this.isLoading = true;

    this.standingService.getBracketWithDetails(this.tournament.id).subscribe({
      next: (bracketStanding) => {
        if (bracketStanding) {
          this.matches = bracketStanding.matches;
          this.standingId = bracketStanding.id;

          // Map backend results to MatchResult structure
          this.matches.forEach(match => {
            match.result = {
              teamAId: match.teamAId,
              teamAWins: match.teamAWins,  // From backend
              teamBId: match.teamBId,
              teamBWins: match.teamBWins   // From backend
            };
          });
        } else {
          this.matches = [];
        }

        this.isLoading = false;

        if (resetUpdatingFlag !== null) {
          this.isUpdating[resetUpdatingFlag] = false;
        }

        if (this.viewInitialized) {
          this.renderBracket();
        }
      },
      error: (err) => {
        console.error('Error loading bracket:', err);
        this.isLoading = false;
        this.matches = [];
        if (resetUpdatingFlag !== null) {
          this.isUpdating[resetUpdatingFlag] = false;
        }
      }
    });
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

      // Add click handlers after rendering completes
      await new Promise(resolve => setTimeout(resolve, 100));
      this.addClickHandlersToParticipants();
    } catch (error) {
      console.error('Error rendering bracket:', error);
    }
  }

  private addClickHandlersToParticipants(): void {
    if (!this.bracketContainer?.nativeElement || !this.standingId) {
      return;
    }

    const isTournamentFinished = this.tournamentState?.currentStatus === TournamentStatus.Finished;
    const containerElement = this.bracketContainer.nativeElement;
    const participantElements = containerElement.querySelectorAll('.participant');

    participantElements.forEach((element: Element) => {
      const htmlElement = element as HTMLElement;
      const participantId = htmlElement.getAttribute('data-participant-id');
      const matchElement = htmlElement.closest('.match');

      if (!matchElement || !participantId) {
        return;
      }

      const matchId = matchElement.getAttribute('data-match-id');
      if (!matchId) {
        return;
      }

      const match = this.matches.find(m => m.id === parseInt(matchId));
      if (!match || match.winnerId || isTournamentFinished) {
        // Match already has winner or tournament is finished - don't add click handler
        htmlElement.style.cursor = 'default';
        return;
      }

      // Add clickable styling
      htmlElement.style.cursor = 'pointer';
      htmlElement.classList.add('clickable-participant');

      // Add click handler
      htmlElement.onclick = (event) => {
        event.stopPropagation();

        const teamId = parseInt(participantId);
        if (!this.isUpdating[match.id]) {
          this.scoreGameForTeam(match.id, teamId);
        }
      };

      // Hover effects now handled by CSS (no inline styles needed)
    });
  }

  private scoreGameForTeam(matchId: number, teamId: number): void {
    const match = this.matches.find(m => m.id === matchId);
    if (!match || match.winnerId || !this.tournament) {
      return;
    }

    this.isUpdating[matchId] = true;

    // Find unfinished game (data already loaded)
    const gameToUpdate = match.games.find(game => !game.winnerId);

    if (!gameToUpdate) {
      console.warn('No unfinished game found for match', matchId);
      this.isUpdating[matchId] = false;
      return;
    }

    const gameResult = {
      gameId: gameToUpdate.id,
      winnerId: teamId,
      teamAScore: undefined,
      teamBScore: undefined,
      matchId: matchId,
      standingId: match.standingId,
      tournamentId: this.tournament.id
    };

    this.matchService.setGameResult(gameResult).subscribe({
      next: (result) => {
        if (result.matchFinished && result.matchWinnerId && result.matchLoserId) {
          match.winnerId = result.matchWinnerId;
          match.loserId = result.matchLoserId;

          this.matchFinished.emit({
            winnerId: result.matchWinnerId,
            loserId: result.matchLoserId,
            allGroupsFinished: result.allGroupsFinished || false,
            tournamentFinished: result.tournamentFinished || false,
            finalStandings: result.finalStandings
          });
        }

        // Single reload call
        this.loadBracket(matchId);
      },
      error: (err) => {
        console.error('Error scoring game:', err);
        this.isUpdating[matchId] = false;
      }
    });
  }
}
