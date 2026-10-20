import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <div class="page-narrow stack">
      <h1>Page not found</h1>
      <p class="muted">The page does not exist or the link is broken.</p>
      <a routerLink="/tournaments">Go to tournaments</a>
    </div>
  `,
})
export class NotFoundPage {}
