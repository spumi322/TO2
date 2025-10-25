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

  addTeamToTournament(tournamentId: number, teamId: number): Observable<any> {
    const request = {
      TournamentId: tournamentId,
      TeamId: teamId
    };
    return this.http.post(`${this.apiUrl}/tournamentId/teamId`, request);
  }

  removeTeamFromTournament(teamId: number, tournamentId: number) {
    return this.http.delete(`${this.apiUrl}/${teamId}/${tournamentId}`);
  }

  // POST /api/teams
  createTeam(name: string): Observable<{ id: number }> {
    const request = {
      Name: name
    };
    return this.http.post<{ id: number }>(this.apiUrl, request);
  }
}
