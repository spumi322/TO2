import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ErrorToast } from '../../core/http/error-toast';
import { Format } from '../../shared/enums';
import { advanceText, groupSeedingText } from '../../shared/format-text';
import { distributeTeams } from '../tournaments/tournament-config';
import { TournamentStore } from './tournament.store';

export interface ConfirmRequest {
  header: string;
  message: string;
  confirmLabel: string;
  danger?: boolean;
  withReason?: boolean;
  run: (reason: string | null) => Promise<void>;
}

/**
 * Stage actions (start groups/bracket, cancel, delete) behind one confirm dialog, shared by the header menu
 * and the Overview's status card. Provided on the shell route next to the store.
 */
@Injectable()
export class TournamentActions {
  private readonly store = inject(TournamentStore);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);
  private readonly errorToast = inject(ErrorToast);

  readonly request = signal<ConfirmRequest | null>(null);
  readonly busy = signal(false);
  // Own delete: the TournamentDeleted event that follows is not "someone else deleted it"
  readonly deletingHere = signal(false);

  startGroups(): void {
    const t = this.store.tournament();
    if (!t) return;
    this.request.set({
      header: 'Start groups',
      message: `${t.currentTeams} teams are drawn into ${groupsText(t.currentTeams, t.numberOfGroups)}, every match best of 3.\nRegistration closes: teams can not be changed afterwards.`,
      confirmLabel: 'Start groups',
      run: async () => {
        await this.store.startGroups();
        await this.router.navigate(['/tournaments', t.id, 'groups']);
      },
    });
  }

  startBracket(): void {
    const t = this.store.tournament();
    if (!t) return;
    const message =
      t.format === Format.BracketOnly
        ? `${t.currentTeams} teams are seeded randomly into the bracket, every match best of 3.\nRegistration closes: teams can not be changed afterwards.`
        : [`${advanceText(t.advancingPerGroup ?? 0)}.`, groupSeedingText(t.numberOfGroups, t.advancingPerGroup ?? 0)].filter(Boolean).join(' ');
    this.request.set({
      header: 'Start bracket',
      message,
      confirmLabel: 'Start bracket',
      run: async () => {
        await this.store.startBracket();
        await this.router.navigate(['/tournaments', t.id, 'bracket']);
      },
    });
  }

  cancel(): void {
    this.request.set({
      header: 'Cancel tournament',
      message: 'The tournament stops and stays read-only. This cannot be undone.',
      confirmLabel: 'Cancel tournament',
      danger: true,
      withReason: true,
      run: (reason) => this.store.cancel(reason),
    });
  }

  delete(): void {
    this.request.set({
      header: 'Delete tournament',
      message: 'Deletes the tournament and its teams that are in no other tournament. This cannot be undone.',
      confirmLabel: 'Delete',
      danger: true,
      run: async () => {
        this.deletingHere.set(true);
        try {
          await this.store.delete();
        } catch (error) {
          this.deletingHere.set(false);
          throw error;
        }
        this.messages.add({ severity: 'success', summary: 'Tournament deleted' });
        await this.router.navigate(['/tournaments']);
      },
    });
  }

  async confirm(reason: string | null): Promise<void> {
    const request = this.request();
    if (!request || this.busy()) return;

    this.busy.set(true);
    try {
      await request.run(reason);
      this.request.set(null);
    } catch (error) {
      this.errorToast.show(error, `${request.header} failed`);
      // The state may have changed meanwhile (409): show what is true now
      await this.store.reloadTournament().catch(() => undefined);
      this.request.set(null);
    } finally {
      this.busy.set(false);
    }
  }

  dismiss(): void {
    if (!this.busy()) this.request.set(null);
  }
}

// "4 groups of 4", or the sizes when the registered teams don't split evenly: "4 groups (3, 3, 2, 2)"
export function groupsText(teams: number, groups: number): string {
  const sizes = distributeTeams(teams, groups);
  const label = groups === 1 ? '1 group' : `${groups} groups`;
  if (!sizes.length) return label;
  return new Set(sizes).size === 1 ? `${label} of ${sizes[0]}` : `${label} (${sizes.join(', ')})`;
}
