import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../api';
import { GameResult, SetGameResultRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class MatchesApi {
  private readonly http = inject(HttpClient);

  setGameResult(request: SetGameResultRequest): Observable<GameResult> {
    return this.http.put<GameResult>(apiUrl(`matches/${request.matchId}/games/${request.gameId}/result`), request);
  }
}
