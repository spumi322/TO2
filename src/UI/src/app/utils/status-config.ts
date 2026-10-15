import { TournamentStatus } from '../models/tournament';

export class StatusConfig {
  static getStatusLabel(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Setup: return 'Setup';
      case TournamentStatus.SeedingGroups: return 'Seeding Groups';
      case TournamentStatus.GroupsInProgress: return 'Groups In Progress';
      case TournamentStatus.GroupsCompleted: return 'Groups Completed';
      case TournamentStatus.SeedingBracket: return 'Seeding Bracket';
      case TournamentStatus.BracketInProgress: return 'Bracket In Progress';
      case TournamentStatus.Finished: return 'Finished';
      case TournamentStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  static getStatusDescription(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Setup: return 'Add teams and configure tournament';
      case TournamentStatus.SeedingGroups: return 'Generating group matches...';
      case TournamentStatus.GroupsInProgress: return 'Group stage in progress - score matches';
      case TournamentStatus.GroupsCompleted: return 'All groups finished - ready to start bracket';
      case TournamentStatus.SeedingBracket: return 'Generating bracket matches...';
      case TournamentStatus.BracketInProgress: return 'Bracket stage in progress - score matches';
      case TournamentStatus.Finished: return 'Tournament complete';
      case TournamentStatus.Cancelled: return 'Tournament cancelled';
      default: return '';
    }
  }

  static getStatusCategory(status: TournamentStatus): 'upcoming' | 'ongoing' | 'finished' | 'cancelled' {
    if (status === TournamentStatus.Setup) return 'upcoming';
    if (status === TournamentStatus.Finished) return 'finished';
    if (status === TournamentStatus.Cancelled) return 'cancelled';
    return 'ongoing';
  }

  static getStatusColor(status: TournamentStatus): string {
    const category = this.getStatusCategory(status);
    if (category === 'upcoming') return '#3b82f6';  // Blue
    if (category === 'ongoing') return '#f59e0b';   // Orange
    if (category === 'cancelled') return '#ef4444'; // Red
    return '#10b981';  // Green
  }

  static getStatusClass(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Setup: return 'status-setup';
      case TournamentStatus.SeedingGroups:
      case TournamentStatus.SeedingBracket: return 'status-seeding';
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.BracketInProgress: return 'status-ongoing';
      case TournamentStatus.Finished: return 'status-finished';
      case TournamentStatus.Cancelled: return 'status-cancelled';
      default: return '';
    }
  }
}
