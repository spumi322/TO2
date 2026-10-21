import { DatePipe } from '@angular/common';
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
import { Format, FORMAT_LABELS, ListSection, STATUS_INFO } from '../../shared/enums';
import { FormatIcon } from '../../shared/format-icon';
import { StatusChip } from '../../shared/status-chip';
import { StageTrack } from './stage-track';

type Tab = 'registration' | 'finished' | 'cancelled';

// Finished tab shows the latest ones until "Show all"
const FINISHED_PREVIEW = 10;

const byNewest = (a: TournamentListItem, b: TournamentListItem) => b.id - a.id;
const byDateDesc = (date: (t: TournamentListItem) => string | null) => (a: TournamentListItem, b: TournamentListItem) =>
  (date(b) ?? '').localeCompare(date(a) ?? '');

@Component({
  selector: 'app-tournament-list-page',
  imports: [DatePipe, RouterLink, Button, Skeleton, EmptyState, FormatIcon, StatusChip, StageTrack],
  templateUrl: './tournament-list-page.html',
  styleUrl: './tournament-list-page.css',
})
export class TournamentListPage implements OnInit {
  private readonly api = inject(TournamentsApi);
  private readonly realtime = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  readonly tournaments = signal<TournamentListItem[] | null>(null);
  readonly error = signal<string | null>(null);
  // null = not picked yet: Registration, or Finished when nothing is in registration
  private readonly pickedTab = signal<Tab | null>(null);
  readonly showAllFinished = signal(false);

  protected readonly formatLabels = FORMAT_LABELS;

  private inSection(section: ListSection): TournamentListItem[] {
    return (this.tournaments() ?? []).filter((t) => STATUS_INFO[t.status].section === section);
  }

  readonly live = computed(() => this.inSection('ongoing').sort(byNewest));
  readonly registration = computed(() => this.inSection('upcoming').sort(byNewest));
  readonly finished = computed(() => this.inSection('finished').sort(byDateDesc((t) => t.finishedAt)));
  readonly cancelled = computed(() => this.inSection('cancelled').sort(byDateDesc((t) => t.cancelledAt)));

  readonly finishedShown = computed(() =>
    this.showAllFinished() ? this.finished() : this.finished().slice(0, FINISHED_PREVIEW),
  );

  readonly tab = computed<Tab>(() => this.pickedTab() ?? (this.registration().length > 0 ? 'registration' : 'finished'));

  readonly tabs = computed(() => [
    { key: 'registration' as Tab, title: 'Registration', count: this.registration().length },
    { key: 'finished' as Tab, title: 'Finished', count: this.finished().length },
    { key: 'cancelled' as Tab, title: 'Cancelled', count: this.cancelled().length },
  ]);

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

  selectTab(tab: Tab): void {
    this.pickedTab.set(tab);
  }

  statusColor(t: TournamentListItem): string {
    return `var(${STATUS_INFO[t.status].color})`;
  }

  // GroupsOnly with several groups has one winner per group
  winnerLabel(t: TournamentListItem): string {
    if (t.winnerName) return t.winnerName;
    return t.format === Format.GroupsOnly ? 'Several group winners' : '-';
  }
}
