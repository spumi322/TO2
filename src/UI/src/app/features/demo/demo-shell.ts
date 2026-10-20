import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { formatHasBracket, formatHasGroups } from '../../shared/enums';
import { FormatBadge } from '../../shared/format-badge';
import { StatusChip } from '../../shared/status-chip';
import { TournamentActions } from '../tournament/tournament-actions';
import { TournamentSnapshot, TournamentStore } from '../tournament/tournament.store';

type DemoFile = Omit<TournamentSnapshot, 'state'>;

/**
 * /demo: a finished tournament from a static JSON file, shown with the real tournament tabs.
 * No login, no API: the store gets a read-only state, so no action or scoring control shows.
 */
@Component({
  selector: 'app-demo-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, FormatBadge, StatusChip],
  providers: [TournamentStore, TournamentActions],
  templateUrl: './demo-shell.html',
  styleUrls: ['../tournament/tournament-shell.css', './demo-shell.css'],
})
export class DemoShell implements OnInit {
  private readonly http = inject(HttpClient);
  readonly store = inject(TournamentStore);
  readonly failed = signal(false);

  readonly tabs = computed(() => {
    const t = this.store.tournament();
    if (!t) return [];
    return [
      { path: 'overview', label: 'Overview' },
      { path: 'teams', label: `Teams (${t.currentTeams})` },
      ...(formatHasGroups(t.format) ? [{ path: 'groups', label: 'Groups' }] : []),
      ...(formatHasBracket(t.format) ? [{ path: 'bracket', label: 'Bracket' }] : []),
      { path: 'results', label: 'Results' },
    ];
  });

  async ngOnInit(): Promise<void> {
    try {
      const demo = await firstValueFrom(this.http.get<DemoFile>('demo-tournament.json'));
      this.store.loadSnapshot({
        ...demo,
        state: {
          currentStatus: demo.tournament.status,
          isTransitionState: false,
          isActiveState: false,
          canScoreMatches: false,
          canModifyTeams: false,
          canCancel: false,
          canDelete: false,
          canStartGroups: false,
          canStartBracket: false,
          version: demo.tournament.version,
        },
      });
    } catch {
      this.failed.set(true);
    }
  }
}
