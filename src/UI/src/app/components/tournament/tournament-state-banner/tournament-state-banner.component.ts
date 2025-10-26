import { Component, Input } from '@angular/core';
import { TournamentStateDTO } from '../../../models/tournament';

@Component({
  selector: 'app-tournament-state-banner',
  templateUrl: './tournament-state-banner.component.html',
  styleUrls: ['./tournament-state-banner.component.css']
})
export class TournamentStateBannerComponent {
  @Input() tournamentState: TournamentStateDTO | null = null;
  @Input() isRegistrationOpen: boolean = false;
}
