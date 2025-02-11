import { Component, Input, OnInit } from '@angular/core';
import { Standing } from '../../../models/standing';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit {
  @Input() brackets: Standing[] = [];
  rounds: any[] = [];

  ngOnInit() {
    this.generateBracket();
  }

  ngOnChanges() {
    this.generateBracket();
  }

  generateBracket(): void {
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
        round.push({
          team1: `Team ${j * 2 + 1}`,
          team2: `Team ${j * 2 + 2}`,
          team1Score: 0,
          team2Score: 0  
        });
      }
      structure.push(round);
    }

    return structure;
  }
}
