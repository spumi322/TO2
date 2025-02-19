import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']  // Fixed to plural "styleUrls"
})
export class NavbarComponent {
  items: MenuItem[] = []; // You can leave this empty if you're using custom start/end facets

  constructor(private router: Router) { }

  goToCreateTournament(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/create-tournament']);
  }

  goToSignIn(event: Event): void {
    event.stopPropagation();
    // Navigate to a sign in route, for example:
    this.router.navigate(['/signin']);
  }
}
