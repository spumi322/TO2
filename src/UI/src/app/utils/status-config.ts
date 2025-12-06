import { TournamentStatus } from '../models/tournament';

export class StatusConfig {
  static getStatusLabel(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Setup: return 'Setup';
      case TournamentStatus.SeedingGroups: return 'Seeding Groups';
      case TournamentStatus.GroupsInProgress: return 'Groups In Progress';
      case TournamentStatus.GroupsCompleted: return 'Groups Finished';
      case TournamentStatus.SeedingBracket: return 'Seeding Bracket';
      case TournamentStatus.BracketInProgress: return 'Bracket In Progress';
      case TournamentStatus.Finished: return 'Finished';
      case TournamentStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  static getStatusCategory(status: TournamentStatus): 'upcoming' | 'ongoing' | 'finished' {
    if (status === TournamentStatus.Setup) return 'upcoming';
    if (status === TournamentStatus.Finished || status === TournamentStatus.Cancelled) return 'finished';
    return 'ongoing';
  }

  static getStatusColor(status: TournamentStatus): string {
    const category = this.getStatusCategory(status);
    if (category === 'upcoming') return '#3b82f6';  // Blue
    if (category === 'ongoing') return '#f59e0b';   // Orange
    return status === TournamentStatus.Cancelled ? '#ef4444' : '#10b981';  // Red or Green
  }
}
