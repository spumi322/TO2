import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Tournament } from '../../models/tournament';

@Injectable({
  providedIn: 'root'
})
export class TournamentService {
  private apiUrl = 'http://localhost:5161/api/tournaments';

  constructor(private http: HttpClient) { }

  getAllTournaments() {
    return this.http.get<Tournament[]>(`${this.apiUrl}/all`);
  }
}
