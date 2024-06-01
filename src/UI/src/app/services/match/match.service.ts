import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Match } from '../../models/match';
import { Observable } from 'rxjs';
import { Team } from '../../models/team';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private apiUrl = 'http://localhost:5161/api/standings';

  constructor(private http: HttpClient) { }

  getMatchesByStandingId(standingId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/${standingId}/matches`);
  }

  getTeamsByStandingId(standingId: number): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/${standingId}/teams`);
  }
}
