import { Component, OnInit } from '@angular/core';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute } from '@angular/router';
import { Observable, from, of } from 'rxjs';
import { catchError, concatMap, finalize, switchMap, tap } from 'rxjs/operators';
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
  tournament: Tournament | null = null;
  standings: Standing[] = [];
  tournamentId: number | null = null;
  groups: Standing[] = [];
  brackets: Standing[] = [];
  displayDialog: boolean = false;
  dialogType!: 'add' | 'remove';
  allTeams: Team[] = [];
  availableTeams: Team[] = [];
  selectedTeam: Team | null = null;
  isReloading: boolean = false;

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
                this.tournament = tournament;
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
      this.allTeams = teams;
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
    this.availableTeams = this.allTeams.filter(team => !this.tournament?.teams.some(t => t.id === team.id));
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
    if (this.selectedTeam && this.tournament) {
      this.tournamentService.addTeam(this.selectedTeam.id, this.tournamentId!).pipe(
        tap(() => {
          this.tournament!.teams.push(this.selectedTeam!);
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
    if (this.selectedTeam && this.tournament) {
      this.tournamentService.removeTeam(this.selectedTeam.id, this.tournamentId!).pipe(
        tap(() => {
          this.tournament!.teams = this.tournament!.teams.filter(team => team.id !== this.selectedTeam!.id);
          this.allTeams.push(this.selectedTeam!);
          this.hideDialog();
        }),
        catchError(error => {
          console.error('Error removing team from tournament', error);
          return of(null);
        })
      ).subscribe();
    }
  }

startTournament(): void {
  if (this.tournamentId) {
    this.tournamentService.startTournament(this.tournamentId).pipe(
      switchMap(() => this.generateGroupMatches()),
      switchMap((standingIds: number[]) => {
        return from(standingIds).pipe(
          concatMap((standingId) => this.generateGames(standingId))
        );
      }),
      catchError(error => {
        console.error('Error in the tournament starting process:', error);
        return of(null);  
      })
    ).subscribe(() => {
      this.reloadTournamentData();
    });
  } else {
    console.log('Tournament ID is null');
  }
}


  generateGroupMatches(): Observable<number[]> {
    if (this.tournamentId) {
      this.isReloading = true;
      return this.standingService.generateGroupMatches(this.tournamentId).pipe(
        tap((standingIds: number[]) => {
          console.log('Group matches generated successfully. Standing IDs:', standingIds);
        }),
        catchError(error => {
          console.error('Error generating group matches:', error);
          this.isReloading = false;
          return of([]);  
        })
      );
    } else {
      return of([]);
    }
  }


  generateGames(standingId: number): Observable<void> {
    return this.standingService.generateGames(standingId).pipe(
      tap(() => {
        console.log(`Games generated for standingId: ${standingId}`);
      }),
      catchError(error => {
        console.error(`Error generating games for standingId: ${standingId}`, error);
        this.isReloading = false;
        return of();
      })
    )
  }

  reloadTournamentData(): void {
    if (this.tournamentId) {
      this.isReloading = true;
      this.tournament$ = this.tournamentService.getTournamentWithTeams(this.tournamentId).pipe(
        tap(tournament => {
          if (tournament) {
            this.tournament = tournament;
            this.loadStandings(this.tournamentId!);
          }
        }),
        catchError(error => {
          console.error('Error reloading tournament data', error);
          return of(null);
        }),
        finalize(() => {
          this.isReloading = false;
        })
      );
    }
  }
}
