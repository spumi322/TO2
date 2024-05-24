import { Component, OnInit } from '@angular/core';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { Standing, StandingType } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Team } from '../../../models/team';
import { TeamService } from '../../../services/team/team.service';

@Component({
  selector: 'app-tournament-details',
  templateUrl: './tournament-details.component.html',
  styleUrls: ['./tournament-details.component.css']
})
export class TournamentDetailsComponent implements OnInit {
  tournament$: Observable<Tournament | null> = of(null);
  standings: Standing[] = [];
  tournamentId: number | null = null;
  groups: Standing[] = [];
  brackets: Standing[] = [];
  displayDialog: boolean = false;
  dialogType!: 'add' | 'remove';
  availableTeams: Team[] = [];
  selectedTeam: Team | null = null;

  constructor(
    private tournamentService: TournamentService,
    private standingService: StandingService,
    private teamService: TeamService,
    private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.tournament$ = this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (id) {
          this.tournamentId = +id;
          return this.tournamentService.getTournamentWithTeams(this.tournamentId).pipe(
            tap(tournament => {
              if (tournament) {
                this.loadStandings(this.tournamentId!);
              }
            }),
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

    this.teamService.getAllTeams().subscribe(teams => {
      this.availableTeams = teams;
    });
  }

  loadStandings(tournamentId: number): void {
    this.standingService.getStandingsByTournamentId(tournamentId).pipe(
      tap((standings: Standing[]) => {
        console.log('Fetched standings:', standings);
        const separatedStandings = this.separateStandingsByType(standings);
        this.groups = separatedStandings.groups;
        this.brackets = separatedStandings.brackets;
      }),
      catchError(error => {
        console.error('Error loading standings', error);
        return of([]);
      })
    ).subscribe();
  }

  separateStandingsByType(standings: Standing[]): { groups: Standing[], brackets: Standing[] } {
    return {
      groups: standings.filter(s => s.type === StandingType.Group),
      brackets: standings.filter(s => s.type === StandingType.Bracket)
    };
  }

  getFormatLabel(format: Format): string {
    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.BracketAndGroups:
        return 'Bracket and Group Stage';
      default:
        return 'Unknown Format';
    }
  }

  showAddTeamDialog(): void {
    this.dialogType = 'add';
    this.displayDialog = true;
  }

  showRemoveTeamDialog(): void {
    this.dialogType = 'remove';
    this.displayDialog = true;
  }

  hideDialog(): void {
    this.displayDialog = false;
    this.selectedTeam = null;
  }

  addTeam(): void {
    if (this.selectedTeam) {
      this.tournamentService.addTeam(this.selectedTeam.id, this.tournamentId!).pipe(
        tap(() => {
          this.loadStandings(this.tournamentId!);
          this.hideDialog();
        }),
        catchError(error => {
          console.error('Error adding team to tournament', error);
          return of(null);
        })
      ).subscribe();
    }
  }

  removeTeam(): void {
    if (this.selectedTeam) {
      this.tournamentService.removeTeam(this.selectedTeam.id, this.tournamentId!).pipe(
        tap(() => {
          this.loadStandings(this.tournamentId!);
          this.hideDialog();
        }),
        catchError(error => {
          console.error('Error removing team from tournament', error);
          return of(null);
        })
      ).subscribe();
    }
  }
}
