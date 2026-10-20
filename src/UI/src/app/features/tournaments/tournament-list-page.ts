import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';
import { debounceTime, merge } from 'rxjs';
import { TournamentsApi } from '../../core/api/tournaments.api';
import { toApiError } from '../../core/http/api-error';
import { TournamentListItem } from '../../core/models';
import { TENANT_EVENT_NAMES } from '../../core/realtime/hub-events';
import { RealtimeService } from '../../core/realtime/realtime.service';
import { EmptyState } from '../../shared/empty-state';
import { ListSection, STATUS_INFO } from '../../shared/enums';
import { FormatBadge } from '../../shared/format-badge';
import { StatusChip } from '../../shared/status-chip';

const SECTIONS: { key: ListSection; title: string }[] = [
  { key: 'ongoing', title: 'Ongoing' },
  { key: 'upcoming', title: 'Registration' },
  { key: 'finished', title: 'Finished' },
  { key: 'cancelled', title: 'Cancelled' },
];

@Component({
  selector: 'app-tournament-list-page',
  imports: [RouterLink, Button, Skeleton, EmptyState, FormatBadge, StatusChip],
  templateUrl: './tournament-list-page.html',
  styleUrl: './tournament-list-page.css',
})
export class TournamentListPage implements OnInit {
  private readonly api = inject(TournamentsApi);
  private readonly realtime = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  readonly tournaments = signal<TournamentListItem[] | null>(null);
  readonly error = signal<string | null>(null);

  readonly sections = computed(() => {
    const tournaments = this.tournaments() ?? [];
    return SECTIONS.map((section) => ({
      ...section,
      items: tournaments
        .filter((t) => STATUS_INFO[t.status].section === section.key)
        .sort((a, b) => b.id - a.id),
    })).filter((section) => section.items.length > 0);
  });

  ngOnInit(): void {
    this.load();

    // Any change in the organization: reload the list (debounced, a batch of events = one reload)
    merge(...TENANT_EVENT_NAMES.map((name) => this.realtime.on(name)), this.realtime.reconnected$)
      .pipe(debounceTime(500), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
  }

  load(): void {
    this.api.list().subscribe({
      next: (tournaments) => {
        this.tournaments.set(tournaments);
        this.error.set(null);
      },
      error: (error: unknown) => this.error.set(toApiError(error).message),
    });
  }
}
