import { inject, Injectable, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ErrorToast } from '../../core/http/error-toast';
import { GameWinnerChange } from './match-card/match-card';
import { TournamentStore } from './tournament.store';

// Shared by the groups and bracket tabs: sends a game result, tracks which match is saving, reports the outcome
@Injectable()
export class Scoring {
  private readonly store = inject(TournamentStore);
  private readonly messages = inject(MessageService);
  private readonly errorToast = inject(ErrorToast);

  readonly savingMatchId = signal<number | null>(null);

  async setWinner(change: GameWinnerChange): Promise<void> {
    if (this.savingMatchId() !== null) return;

    this.savingMatchId.set(change.match.id);
    try {
      const result = await this.store.setGameWinner(change.match, change.game, change.winnerId);
      if (result.tournamentFinished) {
        this.messages.add({ severity: 'success', summary: 'Tournament finished', detail: 'See the Results tab.' });
      } else if (result.allGroupsFinished) {
        this.messages.add({ severity: 'success', summary: 'All groups finished' });
      } else if (result.standingFinished) {
        this.messages.add({ severity: 'success', summary: 'Group finished' });
      }
    } catch (error) {
      // The store already reloaded the standing on a 409
      this.errorToast.show(error, 'Result not saved');
    } finally {
      this.savingMatchId.set(null);
    }
  }
}
