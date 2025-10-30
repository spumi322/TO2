import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Standing } from '../../models/standing';
import { Observable, map } from 'rxjs';
import { Team } from '../../models/team';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StandingService {
  private apiUrl = `${environment.apiUrl}/standings`;
  private apiUrlTeams = `${environment.apiUrl}/teams`;

  constructor(private http: HttpClient) { }

  getStandingsByTournamentId(tournamentId: number): Observable<Standing[]> {
    return this.http.get<Standing[]>(`${this.apiUrl}/${tournamentId}`);
  }

  getTeamsWithStatsByStandingId(standingId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrlTeams}/${standingId}/teams-with-stats`);
  }

  getGroupsWithTeamsByTournamentId(tournamentId: number): Observable<{ standing: Standing; teams: Observable<Team[]> }[]> {
    return this.getStandingsByTournamentId(tournamentId).pipe(
      map(standings => standings.filter(standing => standing.standingType === 1)),
      map(groups =>
        groups.map(group => ({
          standing: group, 
          teams: this.getTeamsWithStatsByStandingId(group.id)
        }))
      )
    );
  }
}
