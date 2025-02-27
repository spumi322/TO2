import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Match } from '../../models/match';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { Team } from '../../models/team';
import { Game } from '../../models/game';
import { Time } from '@angular/common';
import { MatchFinishedIds, MatchResult } from '../../models/matchresult';
import { Gameresult } from '../../models/gameresult';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private apiUrl = 'http://localhost:5161/api/matches';
  public matchUpdated = new BehaviorSubject<Match | null>(null);

  matchUpdated$ = this.matchUpdated.asObservable();

  constructor(private http: HttpClient) { }

  getMatchesByStandingId(standingId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/all/${standingId}`);
  }

  getAllGamesByMatch(matchId: number): Observable<Game[]> {
    return this.http.get<Game[]>(`${this.apiUrl}/games/${matchId}`);
  }

  setGameResult(gameId: number, request: Gameresult): Observable<MatchFinishedIds | null> {
    return this.http.put<MatchFinishedIds | null>(`${this.apiUrl}/${gameId}/result`, request);
  }
}
