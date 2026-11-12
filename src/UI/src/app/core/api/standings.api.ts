import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../api';
import { Bracket, Group } from '../models';

@Injectable({ providedIn: 'root' })
export class StandingsApi {
  private readonly http = inject(HttpClient);

  groups(tournamentId: number): Observable<Group[]> {
    return this.http.get<Group[]>(apiUrl(`standings/${tournamentId}/groups`));
  }

  bracket(tournamentId: number): Observable<Bracket | null> {
    return this.http.get<Bracket | null>(apiUrl(`standings/${tournamentId}/bracket`));
  }
}
