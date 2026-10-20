import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../api';
import {
  CreateTournamentRequest,
  CreateTournamentResponse,
  StartStageResponse,
  Team,
  TeamPlacement,
  Tournament,
  TournamentListItem,
  TournamentState,
} from '../models';

@Injectable({ providedIn: 'root' })
export class TournamentsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<TournamentListItem[]> {
    return this.http.get<TournamentListItem[]>(apiUrl('tournaments'));
  }

  get(id: number): Observable<Tournament> {
    return this.http.get<Tournament>(apiUrl(`tournaments/${id}`));
  }

  state(id: number): Observable<TournamentState> {
    return this.http.get<TournamentState>(apiUrl(`tournaments/${id}/state`));
  }

  teams(id: number): Observable<Team[]> {
    return this.http.get<Team[]>(apiUrl(`tournaments/${id}/teams`));
  }

  finalStandings(id: number): Observable<TeamPlacement[]> {
    return this.http.get<TeamPlacement[]>(apiUrl(`tournaments/${id}/final-standings`));
  }

  create(request: CreateTournamentRequest): Observable<CreateTournamentResponse> {
    return this.http.post<CreateTournamentResponse>(apiUrl('tournaments'), request);
  }

  addTeams(id: number, names: string[]): Observable<Team[]> {
    return this.http.post<Team[]>(apiUrl(`tournaments/${id}/teams`), { names });
  }

  startGroups(id: number): Observable<StartStageResponse> {
    return this.http.post<StartStageResponse>(apiUrl(`tournaments/${id}/start-groups`), {});
  }

  startBracket(id: number): Observable<StartStageResponse> {
    return this.http.post<StartStageResponse>(apiUrl(`tournaments/${id}/start-bracket`), {});
  }

  cancel(id: number, reason: string | null): Observable<void> {
    return this.http.post<void>(apiUrl(`tournaments/${id}/cancel`), { reason });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(apiUrl(`tournaments/${id}`));
  }
}
