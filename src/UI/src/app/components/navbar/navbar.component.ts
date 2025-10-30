import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AuthService } from '../../services/auth/auth.service';
import { User } from '../../models/auth/user.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit, OnDestroy {
  items: MenuItem[] = [];
  currentUser: User | null = null;
  private userSubscription?: Subscription;

  constructor(
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    // Subscribe to user changes
    this.userSubscription = this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  ngOnDestroy(): void {
    this.userSubscription?.unsubscribe();
  }

  goToCreateTournament(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/create-tournament']);
  }

  goToLogin(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/login']);
  }

  goToRegister(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/register']);
  }

  logout(event: Event): void {
    event.stopPropagation();
    this.authService.logout();
  }

  get isAuthenticated(): boolean {
    return this.currentUser !== null;
  }
}
