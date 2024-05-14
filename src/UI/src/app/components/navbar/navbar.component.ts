import { Component } from '@angular/core';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  items: MenuItem[] = [
    { label: 'Home', icon: 'pi pi-home', routerLink: '/' },
    {
      label: 'Tournaments',
      icon: 'pi pi-list',
      items: [
        { label: 'Create', icon: 'pi pi-cog', routerLink: '/create-tournament' },
      ]
    },
  ];
}
