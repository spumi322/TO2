import { Component } from '@angular/core';

@Component({
  selector: 'app-landing',
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent {

  formats = [
    {
      icon: 'pi-table',
      name: 'Groups Only',
      tag: '',
      description: 'Pure round-robin competition. Every team plays every other team. Final standings decided by wins and losses.',
      features: ['Round-robin scheduling', 'Automatic standings', 'Configurable groups', 'Fair tiebreakers']
    },
    {
      icon: 'pi-sitemap',
      name: 'Bracket Only',
      tag: '',
      description: 'Classic single-elimination. Win or go home. Bracket generated automatically from seeded teams.',
      features: ['Auto-generated bracket', 'Best-of series', 'Automatic advancement', 'Clear champion']
    },
    {
      icon: 'pi-star',
      name: 'Groups + Bracket',
      tag: 'Most Popular',
      description: 'The complete package. Group stage determines seeding, top teams advance to the knockout bracket.',
      features: ['Group stage', 'Playoff bracket', 'Auto-seeding from groups', 'Full tournament lifecycle']
    }
  ];

  features = [
    { icon: 'pi-bolt', title: 'Real-Time Updates', description: 'Live scores and bracket changes the moment results are recorded. No refreshing needed.' },
    { icon: 'pi-shield', title: 'Org Isolation', description: 'Your tournaments stay private. Complete data isolation — no cross-tenant access possible.' },
    { icon: 'pi-sitemap', title: 'Auto Brackets', description: 'Bracket generation and team seeding from group results happens automatically.' },
    { icon: 'pi-users', title: 'Team Management', description: 'Register teams, control registration windows, and manage your roster in one place.' },
    { icon: 'pi-chart-bar', title: 'Live Standings', description: 'Automatic standings with win/loss tracking and tiebreaker resolution.' },
    { icon: 'pi-lock', title: 'State Enforcement', description: 'Tournament lifecycle is enforced. Invalid transitions are blocked — no broken states.' }
  ];

  steps = [
    {
      number: '01',
      title: 'Create Your Organization',
      description: 'Sign up and get your private workspace instantly. All your data stays isolated and secure.'
    },
    {
      number: '02',
      title: 'Configure a Tournament',
      description: 'Choose format, set team count, pick best-of series. Ready to go in under a minute.'
    },
    {
      number: '03',
      title: 'Run & Track Live',
      description: 'Add teams, start the tournament, record results. Standings and brackets update automatically.'
    }
  ];
}
