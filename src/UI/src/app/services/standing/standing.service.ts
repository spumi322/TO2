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

  getTeamsByStandingId(standingId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/${standingId}/teams`);
  }

  generateGroupMatches(tournamentId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${tournamentId}/generate-groupmatches`, {});
  }
}
