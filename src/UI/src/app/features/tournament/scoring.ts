import { inject, Injectable, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ErrorToast } from '../../core/http/error-toast';
import { StandingMatch } from '../../core/models';
import { TournamentStore } from './tournament.store';

// Shared by the groups and bracket tabs: confirms a match, tracks which match is saving, reports the outcome
@Injectable()
export class Scoring {
  private readonly store = inject(TournamentStore);
  private readonly messages = inject(MessageService);
  private readonly errorToast = inject(ErrorToast);

  readonly savingMatchId = signal<number | null>(null);

  async confirm(match: StandingMatch): Promise<void> {
    if (this.savingMatchId() !== null) return;

    this.savingMatchId.set(match.id);
    try {
      const result = await this.store.confirmMatch(match);
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
