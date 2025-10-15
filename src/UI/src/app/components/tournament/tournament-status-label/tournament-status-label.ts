import { TournamentStatus } from "../../../models/tournament";

export class TournamentStatusLabel {
  static labels = {
    [TournamentStatus.Setup]: 'Setup',
    [TournamentStatus.SeedingGroups]: 'Seeding Groups',
    [TournamentStatus.GroupsInProgress]: 'Groups In Progress',
    [TournamentStatus.GroupsCompleted]: 'Groups Completed',
    [TournamentStatus.SeedingBracket]: 'Seeding Bracket',
    [TournamentStatus.BracketInProgress]: 'Bracket In Progress',
    [TournamentStatus.Finished]: 'Finished',
    [TournamentStatus.Cancelled]: 'Cancelled',
  };

  static getLabel(status: TournamentStatus): string {
    return this.labels[status] || 'Unknown';
  }
}
