import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { Standing } from '../../../models/standing';
import { Match } from '../../../models/match';
import { Team } from '../../../models/team';
import { Tournament, TournamentStatus } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { StandingService } from '../../../services/standing/standing.service';
import { MatchFinishedIds } from '../../../models/matchresult';
import { forkJoin } from 'rxjs';

interface RoundData {
  roundNumber: number;
  roundName: string;
  matches: Match[];
}

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit, OnChanges {
  @Input() bracket: Standing | null = null;
  @Input() tournament: Tournament | null = null;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();
  @Output() tournamentFinished = new EventEmitter<void>();

  rounds: RoundData[] = [];
  bracketTeams: Team[] = [];
  championTeam: Team | null = null;
  isLoading = false;

  // Constants for template
  TournamentStatus = TournamentStatus;

  constructor(
    private matchService: MatchService,
    private standingService: StandingService
  ) {}

  ngOnInit(): void {
    this.loadBracketData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['bracket'] || changes['tournament']) {
      this.loadBracketData();
    }
  }

  loadBracketData(): void {
    if (!this.bracket?.id) {
      this.rounds = [];
      this.bracketTeams = [];
      return;
    }

    this.isLoading = true;

    forkJoin({
      matches: this.matchService.getMatchesByStandingId(this.bracket.id),
      teams: this.standingService.getTeamsWithStatsByStandingId(this.bracket.id)
    }).subscribe({
      next: ({ matches, teams }) => {
        this.bracketTeams = teams;
        this.organizeBracketRounds(matches);
        this.findChampion();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading bracket data:', error);
        this.isLoading = false;
      }
    });
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
        .filter(m => (m.round || 1) === round)
        .sort((a, b) => (a.seed || 0) - (b.seed || 0));

      if (roundMatches.length > 0) {
        this.rounds.push({
          roundNumber: round,
          roundName: this.getRoundName(round, maxRound),
          matches: roundMatches
        });
      }
    }
  }

  getRoundName(round: number, totalRounds: number): string {
    const matchesInRound = this.rounds.find(r => r.roundNumber === round)?.matches.length || 0;

    // Determine by number of matches in the round
    if (matchesInRound === 1) return 'Finals';
    if (matchesInRound === 2) return 'Semi-Finals';
    if (matchesInRound === 4) return 'Quarter-Finals';
    if (matchesInRound === 8) return 'Round of 16';

    // Fallback to round number
    return `Round ${round}`;
  }

  findChampion(): void {
    this.championTeam = this.bracketTeams.find(t => t.status === 4) || null;
  }

  getTeamName(teamId: number): string {
    const team = this.bracketTeams.find(t => t.id === teamId);
    return team?.name || 'TBD';
  }

  getMatchWins(match: Match, teamId: number): number {
    if (!match.games) return 0;
    return match.games.filter(g => g.winnerId === teamId).length;
  }

  getTeamStatus(teamId: number): number | null {
    const team = this.bracketTeams.find(t => t.id === teamId);
    return team?.status ?? null;
  }

  getStatusLabel(status: number | null): string {
    switch (status) {
      case 4: return 'Champion';
      case 2: return 'Advanced';
      case 3: return 'Eliminated';
      case 1: return 'Competing';
      default: return '';
    }
  }

  getStatusBadgeClass(status: number | null): string {
    switch (status) {
      case 4: return 'status-champion';
      case 2: return 'status-advanced';
      case 3: return 'status-eliminated';
      case 1: return 'status-competing';
      default: return '';
    }
  }

  onMatchFinished(result: MatchFinishedIds): void {
    this.matchFinished.emit(result);

    // Wait for backend to process (generate next round if needed)
    setTimeout(() => {
      this.loadBracketData();

      // Check if tournament finished
      if (this.tournament?.status === TournamentStatus.Finished) {
        this.tournamentFinished.emit();
      }
    }, 800);
  }

  isTournamentFinished(): boolean {
    return this.tournament?.status === TournamentStatus.Finished;
  }
}
