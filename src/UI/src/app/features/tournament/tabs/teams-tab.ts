import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { ErrorToast } from '../../../core/http/error-toast';
import { Team } from '../../../core/models';
import { ConfirmDialog } from '../../../shared/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state';
import { TeamBadge } from '../../../shared/team-badge';
import { TournamentStore } from '../tournament.store';
import { checkTeamNames, parseTeamNames } from './team-names';

@Component({
  selector: 'app-teams-tab',
  imports: [FormsModule, Button, Textarea, ConfirmDialog, EmptyState, TeamBadge],
  templateUrl: './teams-tab.html',
  styleUrl: './teams-tab.css',
})
export class TeamsTab {
  readonly store = inject(TournamentStore);
  private readonly messages = inject(MessageService);
  private readonly errorToast = inject(ErrorToast);

  readonly input = signal('');
  readonly adding = signal(false);
  readonly removing = signal(false);
  readonly teamToRemove = signal<Team | null>(null);

  readonly canEdit = computed(() => !!this.store.state()?.canModifyTeams);
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
  readonly sortedTeams = computed(() => [...this.store.teams()].sort((a, b) => a.name.localeCompare(b.name)));

  async add(): Promise<void> {
    const names = this.names();
    if (!names.length || this.problems().length) return;

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

  async remove(): Promise<void> {
    const team = this.teamToRemove();
    if (!team) return;

    this.removing.set(true);
    try {
      await this.store.removeTeam(team.id);
    } catch (error) {
      this.errorToast.show(error, 'Team not removed');
      await this.store.reloadTeams().catch(() => undefined);
    } finally {
      this.removing.set(false);
      this.teamToRemove.set(null);
    }
  }

  closeRemoveDialog(visible: boolean): void {
    if (!visible && !this.removing()) this.teamToRemove.set(null);
  }
}
