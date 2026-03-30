import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Team } from '../../../models/team';

@Component({
  selector: 'app-team-management',
  templateUrl: './team-management.component.html',
  styleUrls: ['./team-management.component.css']
})
export class TeamManagementComponent implements OnInit {
  team: Team | null = null;
  tournamentId!: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tournamentService: TournamentService
  ) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.paramMap.get('id')!;
    const teamId = +this.route.snapshot.paramMap.get('teamId')!;
    this.tournamentId = id;

    this.tournamentService.getTournamentWithTeams(id).subscribe(tournament => {
      this.team = tournament.teams?.find(t => t.id === teamId) ?? null;
    });
  }

  goBack(): void {
    this.router.navigate(['/tournament', this.tournamentId]);
  }
}
