import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Match } from '../../models/match';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { Team } from '../../models/team';
import { Game } from '../../models/game';
import { Time } from '@angular/common';
import { MatchFinishedIds, MatchResult } from '../../models/matchresult';
import { GameResult, GameProcessResult } from '../../models/gameresult';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private apiUrl = `${environment.apiUrl}/matches`;
  public matchUpdated = new BehaviorSubject<Match | null>(null);

  matchUpdated$ = this.matchUpdated.asObservable();

  constructor(private http: HttpClient) { }

  getMatchesByStandingId(standingId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/all/${standingId}`);
  }

  getAllGamesByMatch(matchId: number): Observable<Game[]> {
    return this.http.get<Game[]>(`${this.apiUrl}/games/${matchId}`);
  }

  setGameResult(request: GameResult): Observable<GameProcessResult> {
    return this.http.put<GameProcessResult>(`${this.apiUrl}/${request.gameId}/result`, request);
  }
}
