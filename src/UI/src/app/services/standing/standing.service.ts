import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Standing } from '../../models/standing';
import { Observable, map } from 'rxjs';
import { Team } from '../../models/team';

@Injectable({
  providedIn: 'root'
})
export class StandingService {
  private apiUrl = 'http://localhost:5161/api/standings';
  private apiUrlTeams = 'http://localhost:5161/api/teams';

  constructor(private http: HttpClient) { }

  getStandingsByTournamentId(tournamentId: number): Observable<Standing[]> {
    return this.http.get<Standing[]>(`${this.apiUrl}/${tournamentId}`);
  }

  getGroupsByTournamentId(tournamentId: number): Observable<Standing[]> {
    return this.getStandingsByTournamentId(tournamentId).pipe(
      map(standings => standings.filter(standing => standing.type === 1))
    );
  }

  getBracketsByTournamentId(tournamentId: number): Observable<Standing[]> {
    return this.getStandingsByTournamentId(tournamentId).pipe(
      map(standings => standings.filter(standing => standing.type === 2))
    );
  }

  generateGroupMatches(tournamentId: number): Observable<number[]> {
    return this.http.post<number[]>(`${this.apiUrl}/${tournamentId}/generate-groupmatches`, {});
  }

  generateGames(standingId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${standingId}/generate-games`, {});
  }

  getTeamsWithStatsByStandingId(standingId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrlTeams}/${standingId}/teams-with-stats`);
  }

  getGroupsWithTeamsByTournamentId(tournamentId: number): Observable<{ standing: Standing; teams: Observable<Team[]> }[]> {
    return this.getGroupsByTournamentId(tournamentId).pipe(
      map(groups =>
        groups.map(group => ({
          standing: group, 
          teams: this.getTeamsWithStatsByStandingId(group.id)
        }))
      )
    );
  }
}
