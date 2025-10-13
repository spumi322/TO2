import { Component, OnInit, AfterViewInit, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Tournament } from '../../models/tournament';
import { Match } from '../../models/match';
import { Standing } from '../../models/standing';
import { TournamentService } from '../../services/tournament/tournament.service';
import { StandingService } from '../../services/standing/standing.service';
import { MatchService } from '../../services/match/match.service';
import { BracketAdapterService } from '../../services/bracket-adapter.service';

@Component({
  selector: 'app-bracket-test',
  templateUrl: './bracket-test.component.html',
  styleUrl: './bracket-test.component.css'
})
export class BracketTestComponent implements OnInit, AfterViewInit {
  // Display state
  libraryLoaded = false;
  elementExists = false;
  renderStatus = 'Not attempted';
  isLoading = false;

  // Data
  tournament?: Tournament;
  standingId?: number;
  matches: Match[] = [];

  constructor(
    private route: ActivatedRoute,
    private tournamentService: TournamentService,
    private standingService: StandingService,
    private matchService: MatchService,
    private bracketAdapter: BracketAdapterService
  ) {}

  ngOnInit() {
    // Check if library is loaded
    this.libraryLoaded = !!(window as any).bracketsViewer;
    console.log('Library loaded:', this.libraryLoaded);
    console.log('window.bracketsViewer:', (window as any).bracketsViewer);

    // Check if we're in route mode (has tournament ID) or standalone test mode
    this.route.paramMap.subscribe(params => {
      const tournamentId = params.get('id');
      if (tournamentId) {
        console.log('Loading tournament:', tournamentId);
        this.loadTournamentAndBracket(+tournamentId);
      } else {
        console.log('Test mode - using hardcoded data');
        // Standalone test mode will render after view init
      }
    });
  }

  ngAfterViewInit() {
    // Check if element exists
    const element = document.querySelector('.brackets-viewer');
    this.elementExists = !!element;
    console.log('Element exists:', this.elementExists);
    console.log('Element:', element);

    // Auto-test only in standalone mode (no tournament)
    if (!this.tournament) {
      setTimeout(() => this.testRender(), 100);
    }
  }

  loadTournamentAndBracket(tournamentId: number) {
    this.isLoading = true;
    this.renderStatus = 'Loading tournament data...';

    // Load tournament
    this.tournamentService.getTournament(tournamentId).subscribe({
      next: (tournament) => {
        console.log('Tournament loaded:', tournament);
        this.tournament = tournament;

        // Load standings to find bracket
        this.standingService.getStandingsByTournamentId(tournamentId).subscribe({
          next: (standings: Standing[]) => {
            console.log('Standings loaded:', standings);
            // StandingType.Bracket = 2 (based on Standing model)
            const bracket = standings.find((s: Standing) => s.standingType === 2);

            if (bracket) {
              this.standingId = bracket.id;
              console.log('Bracket standing found:', bracket);
              this.loadMatches();
            } else {
              this.renderStatus = 'ERROR: No bracket found for tournament';
              this.isLoading = false;
              console.error('No bracket standing found');
            }
          },
          error: (err: any) => {
            this.renderStatus = `ERROR loading standings: ${err.message}`;
            this.isLoading = false;
            console.error('Error loading standings:', err);
          }
        });
      },
      error: (err: any) => {
        this.renderStatus = `ERROR loading tournament: ${err.message}`;
        this.isLoading = false;
        console.error('Error loading tournament:', err);
      }
    });
  }

  loadMatches() {
    if (!this.standingId) {
      this.renderStatus = 'ERROR: No standing ID';
      this.isLoading = false;
      return;
    }

    this.renderStatus = 'Loading matches...';
    this.matchService.getMatchesByStandingId(this.standingId).subscribe({
      next: (matches) => {
        console.log('Matches loaded:', matches);
        this.matches = matches;
        this.isLoading = false;

        if (matches.length === 0) {
          this.renderStatus = 'No matches found - bracket not seeded yet';
          return;
        }

        // Render after short delay to ensure DOM is ready
        setTimeout(() => this.renderWithRealData(), 50);
      },
      error: (err: any) => {
        this.renderStatus = `ERROR loading matches: ${err.message}`;
        this.isLoading = false;
        console.error('Error loading matches:', err);
      }
    });
  }

  renderWithRealData() {
    if (!this.tournament || !this.matches.length) {
      this.renderStatus = 'ERROR: Missing tournament or matches data';
      return;
    }

    console.log('Transforming data with adapter...');
    const data = this.bracketAdapter.transformToBracketsViewer(
      this.tournament,
      this.matches
    );

    if (data) {
      console.log('Adapter returned data, rendering...');
      this.testRender(data);
    } else {
      this.renderStatus = 'ERROR: Adapter returned null';
      console.error('Adapter transformation failed');
    }
  }

  testRender(data?: any) {
    console.log('Starting render...');

    if (!this.libraryLoaded) {
      this.renderStatus = 'ERROR: Library not loaded';
      console.error('Library not available');
      return;
    }

    const element = document.querySelector('.brackets-viewer');
    if (!element) {
      this.renderStatus = 'ERROR: Element not found';
      console.error('Element not found');
      return;
    }

    // Use provided data or fall back to hardcoded test data
    const bracketData = data || this.getHardcodedTestData();

    console.log('Bracket data to render:', bracketData);

    try {
      console.log('Calling bracketsViewer.render()...');
      (window as any).bracketsViewer.render(bracketData, {
        selector: '.brackets-viewer',
        clear: true
      });
      this.renderStatus = this.tournament
        ? 'SUCCESS: Real bracket rendered!'
        : 'SUCCESS: Test bracket rendered!';
      console.log('Render successful!');
    } catch (error) {
      this.renderStatus = `ERROR: ${error}`;
      console.error('Render failed:', error);
    }
  }

  getHardcodedTestData() {
    return {
      stages: [{
        id: 1,
        tournament_id: 1,
        name: 'Test Bracket',
        type: 'single_elimination',
        number: 1,
        settings: { size: 4 }
      }],
      groups: [{
        id: 1,
        stage_id: 1,
        number: 1
      }],
      rounds: [
        { id: 1, stage_id: 1, group_id: 1, number: 1 },
        { id: 2, stage_id: 1, group_id: 1, number: 2 }
      ],
      matches: [
        {
          id: 1,
          stage_id: 1,
          group_id: 1,
          round_id: 1,
          number: 1,
          child_count: 0,
          status: 2, // Ready
          opponent1: { id: 1, score: 0 },
          opponent2: { id: 2, score: 0 }
        },
        {
          id: 2,
          stage_id: 1,
          group_id: 1,
          round_id: 1,
          number: 2,
          child_count: 0,
          status: 2, // Ready
          opponent1: { id: 3, score: 0 },
          opponent2: { id: 4, score: 0 }
        },
        {
          id: 3,
          stage_id: 1,
          group_id: 1,
          round_id: 2,
          number: 1,
          child_count: 0,
          status: 0, // Locked (TBD)
          opponent1: null,
          opponent2: null
        }
      ],
      matchGames: [],
      participants: [
        { id: 1, tournament_id: 1, name: 'Team A' },
        { id: 2, tournament_id: 1, name: 'Team B' },
        { id: 3, tournament_id: 1, name: 'Team C' },
        { id: 4, tournament_id: 1, name: 'Team D' }
      ]
    };
  }
}
