import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../api';

@Injectable({ providedIn: 'root' })
export class TeamsApi {
  private readonly http = inject(HttpClient);

  removeFromTournament(teamId: number, tournamentId: number): Observable<void> {
    return this.http.delete<void>(apiUrl(`teams/${teamId}/${tournamentId}`));
  }
}
