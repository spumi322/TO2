import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Standing } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Observable, catchError, forkJoin, map, of, switchMap, tap } from 'rxjs';
import { Tournament } from '../../../models/tournament';
import { TeamService } from '../../../services/team/team.service';
import { MatchService } from '../../../services/match/match.service';
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
    // Groups auto-update via parent's subscription to groups$
    this.matchFinished.emit(matchUpdate);
  }
}


