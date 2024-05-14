import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Tournament } from '../../models/tournament';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TournamentService {
  private apiUrl = 'http://localhost:5161/api/tournaments';

  constructor(private http: HttpClient) { }

  getAllTournaments() {
    return this.http.get<Tournament[]>(`${this.apiUrl}/all`);
  }

  getTournament(id: number) {
    return this.http.get<Tournament>(`${this.apiUrl}/${id}`);
  }

  createTournament(tournament: Tournament): Observable<Tournament> {
    return this.http.post<Tournament>(this.apiUrl, tournament);
  }
}
