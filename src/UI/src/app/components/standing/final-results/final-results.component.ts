import { Component, Input, OnInit } from '@angular/core';
import { Tournament } from '../../../models/tournament';
import { Team } from '../../../models/team';
import { FinalStanding } from '../../../models/final-standing';
import { TournamentService } from '../../../services/tournament/tournament.service';

@Component({
  selector: 'app-standing-final-results',
  templateUrl: './final-results.component.html',
  styleUrls: ['./final-results.component.css']
})
export class FinalResultsComponent implements OnInit {
  @Input() tournament!: Tournament;

  finalStandings: FinalStanding[] = [];
  champion: Team | null = null;
  isLoading = true;

  constructor(private tournamentService: TournamentService) {}

  ngOnInit(): void {
    this.loadFinalResults();
  }

  loadFinalResults(): void {
    if (!this.tournament?.id) {
      this.isLoading = false;
      return;
    }

    this.isLoading = true;

    // Load champion
    this.tournamentService.getChampion(this.tournament.id).subscribe({
      next: (champion) => {
        this.champion = champion;
      },
      error: (error) => {
        console.error('Error loading champion', error);
        this.champion = null;
      }
    });

    // Load final standings
    this.tournamentService.getFinalStandings(this.tournament.id).subscribe({
      next: (standings) => {
        this.finalStandings = standings;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading final standings', error);
        this.finalStandings = [];
        this.isLoading = false;
      }
    });
  }
}
