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
export class GroupComponent implements OnInit {
  @Input() tournament!: Tournament;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();

  groups: Standing[] = [];

  constructor(
    private standingService: StandingService) { }

  ngOnInit(): void {
    this.refreshGroups();
  }

  refreshGroups(): void {
    if (this.tournament.id) {
      this.standingService.getGroupsWithDetails(this.tournament.id).pipe(
        catchError(() => of([]))
      ).subscribe((groups) => {
        this.groups = groups;
      });
    }
  }

  onMatchFinished(matchUpdate: MatchFinishedIds): void {
    this.refreshGroups();
    this.matchFinished.emit(matchUpdate);
  }
}


