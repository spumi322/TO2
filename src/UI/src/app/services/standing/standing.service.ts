import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Standing } from '../../models/standing';
import { Observable, map, catchError, of } from 'rxjs';
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

  getGroupsWithDetails(tournamentId: number): Observable<Standing[]> {
    return this.http.get<Standing[]>(`${this.apiUrl}/${tournamentId}/groups`);
  }

  getBracketWithDetails(tournamentId: number): Observable<Standing | null> {
    return this.http.get<Standing>(`${this.apiUrl}/${tournamentId}/bracket`).pipe(
      catchError(err => {
        console.error('Error loading bracket with details:', err);
        return of(null);
      })
    );
  }
}
