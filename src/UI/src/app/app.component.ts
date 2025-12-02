import { Component } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  showNavbar = true;

  constructor(
    private router: Router
  ) {
    // Hide navbar on landing, login, and register pages
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        const noNavbarRoutes = ['/', '/login', '/register'];
        this.showNavbar = !noNavbarRoutes.includes(event.url);
      });
  }
}
