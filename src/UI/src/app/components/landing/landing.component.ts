import { Component } from '@angular/core';

@Component({
  selector: 'app-landing',
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent {

  features = [
    {
      icon: 'shield',
      title: 'Multi-Tenant Isolation',
      description: 'Your organization, your data. Complete data isolation ensures your tournaments stay private and secure.'
    },
    {
      icon: 'tournament',
      title: 'Bracket + Groups Support',
      description: 'Run comprehensive tournaments with group stages and playoff brackets, or go straight to elimination.'
    },
    {
      icon: 'auto_awesome',
      title: 'Automated Seeding & Brackets',
      description: 'Automatic bracket generation and team seeding based on group results. No manual work required.'
    }
  ];

  steps = [
    {
      number: '1',
      title: 'Create Organization',
      description: 'Sign up and create your organization in seconds'
    },
    {
      number: '2',
      title: 'Setup Tournament',
      description: 'Configure format, teams, and tournament structure'
    },
    {
      number: '3',
      title: 'Run Matches',
      description: 'Record results and let the system handle the rest'
    }
  ];

  constructor() {}

}
