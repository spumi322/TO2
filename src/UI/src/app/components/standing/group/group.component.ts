import { Component, Input } from '@angular/core';
import { Standing } from '../../../models/standing';

@Component({
  selector: 'app-standing-group',
  templateUrl: './group.component.html',
  styleUrl: './group.component.css'
})
export class GroupComponent{
  @Input() groups: Standing[] = [];
}
