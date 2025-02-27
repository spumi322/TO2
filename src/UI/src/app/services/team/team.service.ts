import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Team } from '../../models/team';

@Injectable({
  providedIn: 'root'
})
export class TeamService {
  private apiUrl = 'http://localhost:5161/api/teams';

  constructor(private http: HttpClient) { }

  // GET /api/teams/all
  getAllTeams(): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/all`);
  }

  getTeamsWithStatsByStandingId(standingId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/${standingId}/teams-with-stats`);
  }

  addTeamToTournament(tournamentId: number, teamId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${tournamentId}/${teamId}`, {});
  }

  // POST /api/teams
  createTeam(request: { name: string }): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.apiUrl, request);
  }
}
