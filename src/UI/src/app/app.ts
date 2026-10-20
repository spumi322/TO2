import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { TopBar } from './layout/top-bar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, TopBar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
