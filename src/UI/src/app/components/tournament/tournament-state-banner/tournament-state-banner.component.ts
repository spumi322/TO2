import { Component, Input } from '@angular/core';
import { TournamentStateDTO } from '../../../models/tournament';
import { StatusConfig } from '../../../utils/status-config';

@Component({
  selector: 'app-tournament-state-banner',
  templateUrl: './tournament-state-banner.component.html',
  styleUrls: ['./tournament-state-banner.component.css']
})
export class TournamentStateBannerComponent {
  @Input() tournamentState: TournamentStateDTO | null = null;
  @Input() isRegistrationOpen: boolean = false;
  protected readonly StatusConfig = StatusConfig;
}
