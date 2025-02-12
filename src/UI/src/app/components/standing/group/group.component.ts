import { Component, Input, OnInit } from '@angular/core';
import { Standing } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';
import { MatchService } from '../../../services/match/match.service';
import { Match } from '../../../models/match';
import { Team } from '../../../models/team';
import { MatchFinishedIds } from '../../../models/matchresult';
import { Tournament } from '../../../models/tournament';

@Component({
  selector: 'app-standing-group',
  templateUrl: './group.component.html',
  styleUrl: './group.component.css'
})
export class GroupComponent implements OnInit {
  @Input() tournament: Tournament | null = null;
  groups$: Observable<Standing[] | null> = of(null);
  groups: Standing[] | null = [];

  constructor(private standingService: StandingService, private matchService: MatchService) { }

  ngOnInit() {
    if (this.tournament && this.tournament.id) {
      this.groups$ = this.standingService.getGroupsByTournamentId(this.tournament.id).pipe(
        switchMap(groups => {
          const groupDetails$ = groups.map(group =>
            this.matchService.getMatchesByStandingId(group.id).pipe(
              switchMap(matches =>
                this.standingService.getTeamsByStandingId(group.id).pipe(
                  map(teams => {
                    const teamStandings = teams.map(team => ({
                      ...team,
                      wins: 0,
                      losses: 0,
                      points: 0
                    }));
                    return { ...group, matches, teams: teamStandings };
                  })
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
    });
  }

  private loadGroupStandings(tournamentId: number): Observable<Standing[]> {
    return this.standingService.getGroupsByTournamentId(tournamentId).pipe(
      switchMap(groups =>
        this.standingService.getStandingsByTournamentId(tournamentId)
      )
    );
  }

  onGameResultUpdated() {
    if (this.tournament && this.tournament.id) {
      this.groups$ = this.loadGroupStandings(this.tournament.id);
      this.groups$.subscribe(groups => {
        this.groups = groups;
      });
    }
  }
}


