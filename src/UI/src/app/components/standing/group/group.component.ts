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

  onGameResultUpdated(result: MatchFinishedIds) {
    if (this.groups) {
      const group = this.groups.find(g => g.teams.some(t => t.id === result.winnerId || t.id === result.loserId));
      if (group) {
        const winner = group.teams.find(t => t.id === result.winnerId);
        const loser = group.teams.find(t => t.id === result.loserId);

        if (winner) {
          winner.wins += 1;
          winner.points += 3;
        }
        if (loser) {
          loser.losses += 1;
        }

        this.calculateStandings(group);
      }
    }
  }

  calculateStandings(group: Standing) {
    group.teams.forEach(team => {
      team.wins = 0;
      team.losses = 0;
      team.points = 0;
    });

    group.matches.forEach(match => {
      const teamA = group.teams.find(t => t.id === match.teamAId);
      const teamB = group.teams.find(t => t.id === match.teamBId);

      if (teamA && teamB) {
        if (match.winnerId === teamA.id) {
          teamA.wins += 1;
          teamA.points += 3; 
          teamB.losses += 1;
        } else if (match.winnerId === teamB.id) {
          teamB.wins += 1;
          teamB.points += 3; 
          teamA.losses += 1;
        }
      }
    });

    group.teams.sort((a, b) =>
      b.points - a.points ||
      b.wins - a.wins ||
      a.losses - b.losses
    );
  }
}


