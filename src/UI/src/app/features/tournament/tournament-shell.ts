import { Component, computed, DestroyRef, effect, inject, input, untracked } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Skeleton } from 'primeng/skeleton';
import { EmptyState } from '../../shared/empty-state';
import { formatHasBracket, formatHasGroups, TournamentStatus } from '../../shared/enums';
import { FormatBadge } from '../../shared/format-badge';
import { StatusChip } from '../../shared/status-chip';
import { TournamentStore } from './tournament.store';

interface Tab {
  path: string;
  label: string;
}

// /tournaments/:id: header + tab strip; each tab is a child route reading the shared store
@Component({
  selector: 'app-tournament-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, Skeleton, EmptyState, FormatBadge, StatusChip],
  providers: [TournamentStore],
  templateUrl: './tournament-shell.html',
  styleUrl: './tournament-shell.css',
})
export class TournamentShell {
  readonly store = inject(TournamentStore);

  // Route param, bound by withComponentInputBinding
  readonly id = input.required<string>();

  // Tab visibility from the format only; what can be done comes from the state flags
  readonly tabs = computed<Tab[]>(() => {
    const tournament = this.store.tournament();
    if (!tournament) return [];
    return [
      { path: 'overview', label: 'Overview' },
      { path: 'teams', label: `Teams (${tournament.currentTeams})` },
      ...(formatHasGroups(tournament.format) ? [{ path: 'groups', label: 'Groups' }] : []),
      ...(formatHasBracket(tournament.format) ? [{ path: 'bracket', label: 'Bracket' }] : []),
      ...(tournament.status === TournamentStatus.Finished ? [{ path: 'results', label: 'Results' }] : []),
    ];
  });

  constructor() {
    effect(() => {
      const id = Number(this.id());
      untracked(() => void this.store.load(id).then(() => this.store.connect()));
    });
    inject(DestroyRef).onDestroy(() => this.store.disconnect());
  }
}
