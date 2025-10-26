import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Tournament, TournamentStateDTO, TournamentStatus, Format } from '../../../models/tournament';
import { TournamentStatusLabel } from '../tournament-status-label/tournament-status-label';

@Component({
  selector: 'app-tournament-header',
  templateUrl: './tournament-header.component.html',
  styleUrls: ['./tournament-header.component.css']
})
export class TournamentHeaderComponent {
  @Input() tournament: Tournament | null = null;
  @Input() tournamentState: TournamentStateDTO | null = null;
  @Input() isReloading: boolean = false;

  @Output() startGroups = new EventEmitter<void>();
  @Output() startTournament = new EventEmitter<void>();
  @Output() startBracket = new EventEmitter<void>();

  // Expose enums for template
  TournamentStatus = TournamentStatus;
  Format = Format;

  getFormatLabel(format: Format): string {
    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.BracketAndGroups:
        return 'Group Stage + Bracket';
      default:
        return 'Unknown Format';
    }
  }

  getStatusClass(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Setup:
        return 'status-setup';
      case TournamentStatus.SeedingGroups:
      case TournamentStatus.SeedingBracket:
        return 'status-seeding';
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.BracketInProgress:
        return 'status-ongoing';
      case TournamentStatus.Finished:
        return 'status-finished';
      case TournamentStatus.Cancelled:
        return 'status-cancelled';
      default:
        return '';
    }
  }

  getStatusLabel(status: TournamentStatus): string {
    return TournamentStatusLabel.getLabel(status);
  }

  onStartGroups(): void {
    this.startGroups.emit();
  }

  onStartTournament(): void {
    this.startTournament.emit();
  }

  onStartBracket(): void {
    this.startBracket.emit();
  }
}
