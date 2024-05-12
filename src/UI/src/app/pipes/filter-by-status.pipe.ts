import { Pipe, PipeTransform } from '@angular/core';
import { Tournament, TournamentStatus } from '../models/tournament';

@Pipe({
  name: 'filterByStatus'
})
export class FilterByStatusPipe implements PipeTransform {
  transform(tournaments: Tournament[], status: TournamentStatus): Tournament[] {
    return tournaments.filter(t => t.status === status);
  }
}
