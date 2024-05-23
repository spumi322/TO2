import { Component, Input, OnInit } from '@angular/core';
import { Standing } from '../../../models/standing';
import { TreeNode } from 'primeng/api';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit {
  @Input() brackets: Standing[] = [];
  rounds: any[] = [];

  ngOnInit() {
    if (this.brackets.length > 0) {
      const maxTeams = this.brackets[0].maxTeams;
      this.rounds = this.createBracketStructure(maxTeams);
    }
  }

  createBracketStructure(maxTeams: number): any[] {
    const rounds = Math.ceil(Math.log2(maxTeams));
    const structure = [];

    for (let i = 0; i < rounds; i++) {
      const matches = Math.pow(2, rounds - i - 1);
      const round = [];
      for (let j = 0; j < matches; j++) {
        round.push({ team1: 'TBA', team2: 'TBA' });
      }
      structure.push(round);
    }

    return structure;
  }
}

