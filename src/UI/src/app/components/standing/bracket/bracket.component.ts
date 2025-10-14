import { Component, Input, OnInit, OnChanges, AfterViewInit, ChangeDetectorRef } from '@angular/core';
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

  matches: Match[] = [];
  isLoading = true;
  private viewInitialized = false;

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

    try {
      // Call library to render - it creates all HTML/CSS
      (window as any).bracketsViewer.render(data, {
        selector: '.brackets-viewer',
        clear: true
      });
      console.log('Bracket rendered successfully!');
    } catch (error) {
      console.error('Error rendering bracket:', error);
    }
  }
}
