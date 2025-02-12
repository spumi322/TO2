import { Component, Input, OnInit } from '@angular/core';
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

  groups$: Observable<Standing[]> = of([]);
  groups: Standing[] = [];

  constructor(
    private standingService: StandingService,
    private matchService: MatchService) { }

  ngOnInit(): void {
    if (this.tournament.id) {
      this.groups$ = this.standingService.getGroupsWithTeamsByTournamentId(this.tournament.id).pipe(
        switchMap((groupsWithTeams) => {
          const groupDetails$ = groupsWithTeams.map(({ standing, teams }) =>
            forkJoin({
              teams,
              matches: this.matchService.getMatchesByStandingId(standing.id)
            }).pipe(
              map(({ teams, matches }) => ({
                ...standing,
                teams: teams ?? [],
                matches: matches ?? []
              }))
            )
          );

          return forkJoin(groupDetails$);
        }),
        catchError(() => of([]))
      );

      this.groups$.subscribe((groups) => {
        this.groups = groups;
      });
    }
  }

  onMatchFinished(matchUpdate: MatchFinishedIds): void {
    console.log('Match Finished:', matchUpdate);

    this.groups = this.groups.map(group => ({
      ...group,
      matches: group.matches.map(match =>
        match.id === matchUpdate.id
          ? { ...match, winnerId: matchUpdate.winnerId, loserId: matchUpdate.loserId }
          : match
      )
    }));
  }
}


