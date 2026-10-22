import { Component, computed, DestroyRef, effect, inject, input, untracked } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MenuItem, MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { Skeleton } from 'primeng/skeleton';
import { SessionStore } from '../../core/auth/session.store';
import { ConfirmDialog } from '../../shared/confirm-dialog';
import { EmptyState } from '../../shared/empty-state';
import { formatHasBracket, formatHasGroups, TournamentStatus } from '../../shared/enums';
import { FormatBadge } from '../../shared/format-badge';
import { StatusChip } from '../../shared/status-chip';
import { TournamentActions } from './tournament-actions';
import { TournamentStore } from './tournament.store';

interface Tab {
  path: string;
  label: string;
}

// /tournaments/:id: header + tab strip; each tab is a child route reading the shared store
@Component({
  selector: 'app-tournament-shell',
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    Button,
    Menu,
    Skeleton,
    ConfirmDialog,
    EmptyState,
    FormatBadge,
    StatusChip,
  ],
  providers: [TournamentStore, TournamentActions],
  templateUrl: './tournament-shell.html',
  styleUrl: './tournament-shell.css',
})
export class TournamentShell {
  readonly store = inject(TournamentStore);
  readonly actions = inject(TournamentActions);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  // Route param, bound by withComponentInputBinding
  readonly id = input.required<string>();

  // Tab visibility from the format only; what can be done comes from the state flags
  readonly tabs = computed<Tab[]>(() => {
    const tournament = this.store.tournament();
    if (!tournament) return [];
    return [
      { path: 'overview', label: 'Overview' },
      ...(formatHasGroups(tournament.format) ? [{ path: 'groups', label: 'Groups' }] : []),
      ...(formatHasBracket(tournament.format) ? [{ path: 'bracket', label: 'Bracket' }] : []),
      ...(tournament.status === TournamentStatus.Finished ? [{ path: 'results', label: 'Results' }] : []),
    ];
  });

  // Destructive actions live in the menu; start actions are on the Overview's status card
  readonly menuItems = computed<MenuItem[]>(() => {
    const state = this.store.state();
    const items: MenuItem[] = [];
    if (state?.canCancel) items.push({ label: 'Cancel tournament', icon: 'pi pi-ban', command: () => this.actions.cancel() });
    if (state?.canDelete) items.push({ label: 'Delete tournament', icon: 'pi pi-trash', command: () => this.actions.delete() });
    return items;
  });

  constructor() {
    effect(() => {
      const id = Number(this.id());
      untracked(() => void this.store.load(id).then(() => this.store.connect()));
    });
    inject(DestroyRef).onDestroy(() => this.store.disconnect());

    // Deleted by someone else while open: leave the page
    effect(() => {
      const deletedBy = this.store.deletedBy();
      if (!deletedBy || untracked(() => this.actions.deletingHere())) return;
      const who = deletedBy === this.session.session()?.userName ? 'you (another tab)' : deletedBy;
      untracked(() => {
        this.messages.add({ severity: 'warn', summary: 'Tournament deleted', detail: `Deleted by ${who}.` });
        void this.router.navigate(['/tournaments']);
      });
    });
  }

  setDialogVisible(visible: boolean): void {
    if (!visible) this.actions.dismiss();
  }
}
