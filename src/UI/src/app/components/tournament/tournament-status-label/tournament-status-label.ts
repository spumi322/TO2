import { TournamentStatus } from "../../../models/tournament";

export class TournamentStatusLabel {
  static labels = {
    [TournamentStatus.Upcoming]: 'Upcoming',
    [TournamentStatus.Ongoing]: 'Ongoing',
    [TournamentStatus.Finished]: 'Finished',
    [TournamentStatus.Cancelled]: 'Cancelled',
  };

  static getLabel(status: TournamentStatus): string {
    return this.labels[status];
  }
}
