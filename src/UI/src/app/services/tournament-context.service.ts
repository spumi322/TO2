import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { Tournament, TournamentStateDTO } from '../models/tournament';

@Injectable({ providedIn: 'root' })
export class TournamentContextService {
  // Tournament and state data
  private tournamentSubject = new BehaviorSubject<Tournament | null>(null);
  private stateSubject = new BehaviorSubject<TournamentStateDTO | null>(null);

  tournament$: Observable<Tournament | null> = this.tournamentSubject.asObservable();
  state$: Observable<TournamentStateDTO | null> = this.stateSubject.asObservable();

  // Action subjects for navbar buttons
  private startGroupsSubject = new Subject<void>();
  private startTournamentSubject = new Subject<void>();
  private startBracketSubject = new Subject<void>();

  startGroups$ = this.startGroupsSubject.asObservable();
  startTournament$ = this.startTournamentSubject.asObservable();
  startBracket$ = this.startBracketSubject.asObservable();

  setTournament(tournament: Tournament | null): void {
    this.tournamentSubject.next(tournament);
  }

  setState(state: TournamentStateDTO | null): void {
    this.stateSubject.next(state);
  }

  clear(): void {
    this.tournamentSubject.next(null);
    this.stateSubject.next(null);
  }

  triggerStartGroups(): void {
    this.startGroupsSubject.next();
  }

  triggerStartTournament(): void {
    this.startTournamentSubject.next();
  }

  triggerStartBracket(): void {
    this.startBracketSubject.next();
  }
}
