import { Component, OnDestroy, OnInit } from '@angular/core';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute } from '@angular/router';
import { Observable, Subscription, catchError, of, switchMap } from 'rxjs';

@Component({
  selector: 'app-tournament-details',
  templateUrl: './tournament-details.component.html',
  styleUrl: './tournament-details.component.css'
})

export class TournamentDetailsComponent implements OnInit {
  tournament$: Observable<Tournament | null > = of(null);

  constructor(
    private tournamentService: TournamentService,
    private route: ActivatedRoute)
  { }

  ngOnInit(): void {
    this.tournament$ = this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (id) {
          return this.tournamentService.getTournament(+id).pipe(
            catchError(error => {
              console.error('Error loading tournament', error);
              return of(null);
            })
          );
        } else {
          return of(null);
        }
      })
    );
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
