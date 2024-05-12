import { AfterViewInit, Component } from '@angular/core';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-tournament-details',
  templateUrl: './tournament-details.component.html',
  styleUrl: './tournament-details.component.css'
})
export class TournamentDetailsComponent implements AfterViewInit{
  tournament: Tournament | undefined;


  constructor(
    private route: ActivatedRoute,
    private tournamentService: TournamentService
  ) { }

  ngAfterViewInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.tournamentService.getTournament(+id).subscribe(tournament => this.tournament = tournament);
    }
  }

  getFormatLabel(format: Format): string {
    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.BracketAndGroup:
        return 'Bracket and Group Stage';
      default:
        return 'Unknown Format';
    }
  }
}
