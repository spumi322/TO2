import { Component, Input, OnInit, OnChanges, AfterViewInit, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { Tournament } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { BracketAdapterService } from '../../../services/bracket-adapter.service';
import { Match } from '../../../models/match';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit, OnChanges, AfterViewInit {
  @Input() tournament!: Tournament;
  @Input() standingId?: number;
  @ViewChild('bracketContainer', { static: false }) bracketContainer?: ElementRef;

  matches: Match[] = [];
  isLoading = true;
  private viewInitialized = false;
  private renderAttempted = false;

  constructor(
    private matchService: MatchService,
    private bracketAdapter: BracketAdapterService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadMatches();
  }

  ngOnChanges() {
    if (this.viewInitialized) {
      this.loadMatches();
    }
  }

  ngAfterViewInit() {
    this.viewInitialized = true;
    // If matches were already loaded before view init, render now
    if (this.matches.length > 0) {
      this.renderBracket();
    }
  }

  /**
   * Load matches from backend
   */
  loadMatches() {
    if (!this.standingId) {
      console.warn('No standingId provided');
      this.isLoading = false;
      return;
    }

    this.isLoading = true;

    this.matchService.getMatchesByStandingId(this.standingId).subscribe({
      next: (matches) => {
        console.log('Matches loaded:', matches.length);
        this.matches = matches;
        this.isLoading = false;

        // Only render if view is initialized (tab is visible)
        if (this.viewInitialized) {
          // Small delay to ensure DOM is updated
          setTimeout(() => this.renderBracket(), 50);
        }
      },
      error: (err) => {
        console.error('Error loading matches:', err);
        this.isLoading = false;
      }
    });
  }

  /**
   * Render bracket using brackets-viewer.js library
   * The library does ALL the rendering (HTML/CSS)
   * We just provide the data structure
   */
  renderBracket() {
    // Check if library is loaded
    if (!(window as any).bracketsViewer) {
      console.error('brackets-viewer library not loaded! Check angular.json configuration');
      return;
    }

    // Check if we have matches to display
    if (!this.matches || this.matches.length === 0) {
      console.warn('No matches to render - bracket not seeded yet');
      return;
    }

    // Transform data using adapter
    const data = this.bracketAdapter.transformToBracketsViewer(
      this.tournament,
      this.matches
    );

    // Check if adapter returned valid data
    if (!data) {
      console.warn('Adapter returned null - cannot render bracket');
      return;
    }

    console.log('Calling bracketsViewer.render() - library will handle all rendering');

    // Check if the container element is available via ViewChild
    if (!this.bracketContainer?.nativeElement) {
      console.warn('Bracket container not available - ViewChild not initialized or tab not visible');

      // Retry after a delay (tab might become visible soon)
      if (!this.renderAttempted) {
        this.renderAttempted = true;
        console.log('Scheduling retry in 500ms...');
        setTimeout(() => {
          this.renderAttempted = false;
          this.renderBracket();
        }, 500);
      }
      return;
    }

    console.log('Container element found:', this.bracketContainer.nativeElement);
    console.log('Data being passed to bracketsViewer.render():', data);
    console.log('Data structure:', {
      stages: data.stages?.length || 0,
      groups: data.groups?.length || 0,
      rounds: data.rounds?.length || 0,
      matches: data.matches?.length || 0,
      participants: data.participants?.length || 0
    });

    try {
      // Call library to render - it creates all HTML/CSS
      // The library NEEDS groups and rounds arrays since matches reference them
      const viewerData = {
        stages: data.stages,
        groups: data.groups,
        rounds: data.rounds,
        matches: data.matches,
        matchGames: data.matchGames || [],
        participants: data.participants
      };

      console.log('Rendering bracket with data:', viewerData);

      (window as any).bracketsViewer.render(viewerData, {
        selector: '.brackets-viewer',
        clear: true
      });

      console.log('Bracket render() called successfully - library is building DOM asynchronously');
    } catch (error) {
      console.error('Error rendering bracket:', error);
      console.error('Full error object:', error);
    }
  }

  private getHardcodedTestData() {
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
          status: 2,
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
          status: 2,
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
          status: 0,
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
