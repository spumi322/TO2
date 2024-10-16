import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Tournament } from '../../models/tournament';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { Team } from '../../models/team';

@Injectable({
  providedIn: 'root'
})
export class TournamentService {
  private apiUrl = 'http://localhost:5161/api/tournaments';

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

  // POST /api/tournaments/{teamId}/{tournamentId}
  addTeam(teamId: number, tournamentId: number) {
    return this.http.post(`${this.apiUrl}/${teamId}/${tournamentId}`, {});
  }

  // DELETE /api/tournaments/{teamId}/{tournamentId}
  removeTeam(teamId: number, tournamentId: number) {
    return this.http.delete(`${this.apiUrl}/${teamId}/${tournamentId}`);
  }

  // PUT /api/tournaments/{tournamentId}/start
  startTournament(tournamentId: number) {
    return this.http.put(`${this.apiUrl}/${tournamentId}/start`, {});
  }
}
