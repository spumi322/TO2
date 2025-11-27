import { Component, Input } from '@angular/core';
import { Tournament, Format } from '../../../models/tournament';
import { Standing } from '../../../models/standing';
import { FormatConfig } from '../../../utils/format-config';

@Component({
  selector: 'app-tournament-info-card',
  templateUrl: './tournament-info-card.component.html',
  styleUrls: ['./tournament-info-card.component.css']
})
export class TournamentInfoCardComponent {
  @Input() tournament: Tournament | null = null;
  @Input() groups: Standing[] = [];
  @Input() brackets: Standing[] = [];

  // Expose enum for template
  Format = Format;

  getFormatLabel(format: Format): string {
    return FormatConfig.getFormatLabel(format);
  }

  getTeamsPerGroup(): number | null {
    return this.groups.length > 0 ? this.groups[0].maxTeams : null;
  }

  getTeamsPerBracket(): number | null {
    return this.brackets.length > 0 ? this.brackets[0].maxTeams : null;
  }
}
