import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { RealtimeService } from './core/realtime/realtime.service';
import { TopBar } from './layout/top-bar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, TopBar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  // Created here so the hub connection follows the session from app start
  private readonly realtime = inject(RealtimeService);
}
