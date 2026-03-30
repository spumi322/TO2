import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Standing } from '../../../models/standing';
import { Tournament } from '../../../models/tournament';
import { MatchFinishedIds } from '../../../models/matchresult';

@Component({
  selector: 'app-standing-group',
  templateUrl: './group.component.html',
  styleUrl: './group.component.css'
})
export class GroupComponent {
  @Input() tournament!: Tournament;
  @Input() groups: Standing[] = [];
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();

  onMatchFinished(matchUpdate: MatchFinishedIds): void {
    this.matchFinished.emit(matchUpdate);
  }
}
