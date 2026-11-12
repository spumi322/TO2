import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { ErrorToast } from '../../../core/http/error-toast';
import { Team } from '../../../core/models';
import { TournamentStore } from '../tournament.store';
import { teamEmblem, TeamEmblem } from './team-emblem';
import { checkTeamNames, MAX_TEAM_NAME_LENGTH, parseTeamNames } from './team-names';

// Registration on the Overview: add teams in bulk, remove one with a click. Shown only while teams can change.
// One slot per place: teams in registration order, then the open places.
@Component({
  selector: 'app-team-manager',
  imports: [FormsModule, Button, Textarea],
  templateUrl: './team-manager.html',
  styleUrl: './team-manager.css',
})
export class TeamManager {
  readonly store = inject(TournamentStore);
  private readonly messages = inject(MessageService);
  private readonly errorToast = inject(ErrorToast);

  readonly input = signal('');
  readonly adding = signal(false);
  readonly removing = signal<ReadonlySet<number>>(new Set());
  protected readonly maxNameLength = MAX_TEAM_NAME_LENGTH;

  readonly maxTeams = computed(() => this.store.tournament()?.maxTeams ?? 0);
  readonly freeSlots = computed(() => Math.max(0, this.maxTeams() - this.store.teams().length));
  readonly names = computed(() => parseTeamNames(this.input()));
  readonly problems = computed(() =>
    checkTeamNames(
      this.names(),
      this.store.teams().map((team) => team.name),
      this.freeSlots(),
    ),
  );
  readonly slots = computed<({ team: Team; emblem: TeamEmblem } | null)[]>(() => {
    const teams = [...this.store.teams()].sort((a, b) => a.id - b.id).map((team) => ({ team, emblem: teamEmblem(team.name) }));
    return [...teams, ...Array<null>(this.freeSlots()).fill(null)];
  });

  async add(): Promise<void> {
    const names = this.names();
    if (this.adding() || !names.length || this.problems().length) return;

    this.adding.set(true);
    try {
      const added = await this.store.addTeams(names);
      this.input.set('');
      this.messages.add({
        severity: 'success',
        summary: added.length === 1 ? 'Team added' : `${added.length} teams added`,
      });
    } catch (error) {
      this.errorToast.show(error, 'Teams not added');
      await this.store.reloadTeams().catch(() => undefined);
    } finally {
      this.adding.set(false);
    }
  }

  // Enter adds; Shift+Enter is a new line
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void this.add();
    }
  }

  // No confirm: a team is just a name, adding it back is quick
  async remove(team: Team): Promise<void> {
    if (this.removing().has(team.id)) return;

    this.removing.update((ids) => new Set(ids).add(team.id));
    try {
      await this.store.removeTeam(team.id);
    } catch (error) {
      this.errorToast.show(error, 'Team not removed');
      await this.store.reloadTeams().catch(() => undefined);
    } finally {
      this.removing.update((ids) => {
        const next = new Set(ids);
        next.delete(team.id);
        return next;
      });
    }
  }
}
