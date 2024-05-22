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
  maxTeams: number = 16;
  data: TreeNode[] = [];

  ngOnInit(): void {
    this.maxTeams = this.brackets[0]?.maxTeams;
    this.data = this.createBracketTree(this.maxTeams);
  }

  createBracketTree(maxTeams: number): TreeNode[] {
    const rounds = Math.ceil(Math.log2(maxTeams));
    const createNode = (roundIndex: number, matchIndex: number): TreeNode => {
      const label = `Match ${roundIndex}-${matchIndex}`;
      if (roundIndex === rounds - 1) {
        return {
          label: label,
          data: `TBA vs TBA`,
        };
      }

      return {
        label: label,
        expanded: true,
        data: `TBA vs TBA`,
        children: [
          createNode(roundIndex + 1, matchIndex * 2),
          createNode(roundIndex + 1, matchIndex * 2 + 1),
        ],
      };
    };

    const root: TreeNode = {
      label: 'Bracket',
      expanded: true,
      children: [],
    };

    for (let i = 0; i < maxTeams / 2; i++) {
      root.children!.push(createNode(0, i));
    }

    return [root];
  }
}
