import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Match } from '../../models/match';
import { Observable, map } from 'rxjs';
import { Team } from '../../models/team';
import { Game } from '../../models/game';
import { Time } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private apiUrl = 'http://localhost:5161/api/matches';

  constructor(private http: HttpClient) { }

  getMatchesByStandingId(standingId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/all/${standingId}`);
  }

  getAllGamesByMatch(matchId: number): Observable<Game[]> {
    return this.http.get<Game[]>(`${this.apiUrl}/games/${matchId}`);
  }

  setGameResult(gameId: number, teamAScore: number, teamBScore: number, duration: Time, winnerId: number): Observable<number | null> {
    return this.http.put<{ winnerId: number | null }>(`${this.apiUrl}/${gameId}/result`, { teamAScore, teamBScore, duration, winnerId })
      .pipe(
        map(response => response.winnerId)
      );
  }
}
