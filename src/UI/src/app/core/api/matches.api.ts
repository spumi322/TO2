import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../api';
import { GameResult, SetMatchResultRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class MatchesApi {
  private readonly http = inject(HttpClient);

  // Saves a confirmed match result (drafts before it are only shared through the hub)
  setMatchResult(request: SetMatchResultRequest): Observable<GameResult> {
    return this.http.post<GameResult>(apiUrl(`matches/${request.matchId}/result`), request);
  }
}
