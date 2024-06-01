import { Component, Input, OnInit } from '@angular/core';
import { Standing } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';
import { MatchService } from '../../../services/match/match.service';
import { Match } from '../../../models/match';
import { Team } from '../../../models/team';

@Component({
  selector: 'app-standing-group',
  templateUrl: './group.component.html',
  styleUrl: './group.component.css'
})
export class GroupComponent implements OnInit {
  @Input() tournamentId: number | null = null;
  groups$: Observable<Standing[] | null> = of(null);
  groups: Standing[] | null = [];
  matches: Match[] = [];
  teams: Team[] = [];

  constructor(private standingService: StandingService, private matchService: MatchService) { }

  ngOnInit() {
    if (this.tournamentId !== null) {
      this.groups$ = this.standingService.getGroupsByTournamentId(this.tournamentId).pipe(
        switchMap(groups => {
          const groupDetails$ = groups.map(group =>
            this.matchService.getMatchesByStandingId(group.id).pipe(
              switchMap(matches =>
                this.matchService.getTeamsByStandingId(group.id).pipe(
                  map(teams => ({ ...group, matches, teams }))
                )
              )
            )
          );
          return forkJoin(groupDetails$);
        })
      );
    }

    this.groups$.subscribe(groups => {
      this.groups = groups;
      if (groups) {
        this.teams = groups.reduce((acc, group) => acc.concat(group.teams), [] as Team[]);
      }
    });
  }
}


