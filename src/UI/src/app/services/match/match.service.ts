import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Match } from '../../models/match';
import { Observable } from 'rxjs';
import { Game } from '../../models/game';
import { GameResult, GameProcessResult } from '../../models/gameresult';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private apiUrl = `${environment.apiUrl}/matches`;
  constructor(private http: HttpClient) { }

  getMatchesByStandingId(standingId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}?standingId=${standingId}`);
  }

  getAllGamesByMatch(matchId: number): Observable<Game[]> {
    return this.http.get<Game[]>(`${this.apiUrl}/${matchId}/games`);
  }

  setGameResult(request: GameResult): Observable<GameProcessResult> {
    return this.http.put<GameProcessResult>(`${this.apiUrl}/${request.matchId}/games/${request.gameId}/result`, request);
  }
}
