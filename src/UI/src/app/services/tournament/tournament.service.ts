import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { StartBracketResponse, StartGroupsResponse, Tournament, TournamentStateDTO } from '../../models/tournament';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { Team } from '../../models/team';
import { FinalStanding } from '../../models/final-standing';

@Injectable({
  providedIn: 'root'
})
export class TournamentService {
  private apiUrl = `${environment.apiUrl}/tournaments`;

  constructor(private http: HttpClient) { }

  // GET /api/tournaments/all
  getAllTournaments(): Observable<Tournament[]> {
    return this.http.get<Tournament[]>(`${this.apiUrl}/all`);
  }

  // GET /api/tournaments/{id}
  getTournament(id: number): Observable<Tournament> {
    return this.http.get<Tournament>(`${this.apiUrl}/${id}`);
  }

  getTournamentWithTeams(id: number): Observable<Tournament> {
    return this.getTournament(id).pipe(
      switchMap(tournament =>
        this.getAllTeamsByTournamentId(tournament.id).pipe(
          map(teams => ({ ...tournament, teams }))
        )
      )
    );
  }

  // POST /api/tournaments
  createTournament(tournament: Tournament): Observable<Tournament> {
    return this.http.post<Tournament>(this.apiUrl, tournament);
  }

  // GET /api/tournaments/{id}/teams
  getAllTeamsByTournamentId(tournamentId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/${tournamentId}/teams`);
  }


  getAllTournamentsWithTeams(): Observable<Tournament[]> {
    return this.getAllTournaments().pipe(
      switchMap(tournaments => {
        const tournamentRequests = tournaments.map(tournament =>
          this.getAllTeamsByTournamentId(tournament.id).pipe(
            map(teams => ({ ...tournament, teams })),
            catchError(error => {
              console.error(`Error fetching teams for tournament ${tournament.id}:`, error);
              return of({ ...tournament, teams: [] });
            })
          )
        );
        return forkJoin(tournamentRequests);
      }),
      catchError(error => {
        console.error('Error fetching tournaments:', error);
        return of([]);
      })
    );
  }

  // PUT /api/tournaments/{tournamentId}/start
  startTournament(tournamentId: number) {
    return this.http.put(`${this.apiUrl}/${tournamentId}/start`, {});
  }

  startGroups(tournamentId: number): Observable<StartGroupsResponse> {
    return this.http.post<StartGroupsResponse>(
      `${this.apiUrl}/${tournamentId}/start-groups`,
      {}
    );
  }

  getTournamentState(tournamentId: number): Observable<TournamentStateDTO> {
    return this.http.get<TournamentStateDTO>(
      `${this.apiUrl}/${tournamentId}/state`
    );
  }

  startBracket(tournamentId: number): Observable<StartBracketResponse> {
    return this.http.post<StartBracketResponse>(
      `${this.apiUrl}/${tournamentId}/start-bracket`,
      {}
    );
  }

  // GET /api/tournaments/{id}/final-standings
  getFinalStandings(tournamentId: number): Observable<FinalStanding[]> {
    return this.http.get<FinalStanding[]>(`${this.apiUrl}/${tournamentId}/final-standings`).pipe(
      catchError(() => of([]))
    );
  }
}
