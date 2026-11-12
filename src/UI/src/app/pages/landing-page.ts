import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { SessionStore } from '../core/auth/session.store';

@Component({
  selector: 'app-landing-page',
  imports: [RouterLink, Button],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
})
export class LandingPage {
  readonly session = inject(SessionStore);

  readonly formats = [
    {
      icon: 'pi-table',
      name: 'Groups',
      description: 'Round robin: every team plays every other team in its group. The group table is the result.',
      features: ['Round robin schedule', 'Automatic standings', 'Several groups', 'Tie-break by game difference and head-to-head'],
    },
    {
      icon: 'pi-sitemap',
      name: 'Bracket',
      description: 'Single elimination: win or go home. The bracket is drawn at random, uneven team counts get BYEs.',
      features: ['Random draw', 'Automatic advancement', 'BYEs for uneven counts', 'Shared ranks per round'],
    },
    {
      icon: 'pi-star',
      name: 'Groups + Bracket',
      description: 'Group stage first, then the best teams of each group play a knockout bracket.',
      features: ['Seeding from group results', 'Group winners get the BYEs', 'Same-group teams apart in round 1', 'Automatic bracket draw'],
    },
  ];

  readonly features = [
    { icon: 'pi-bolt', title: 'Live updates', description: 'Scores and brackets update on every open screen the moment a result is saved.' },
    { icon: 'pi-shield', title: 'Private organizations', description: 'Each organization only sees its own tournaments. Members join with an invite code.' },
    { icon: 'pi-pencil', title: 'Live scoring', description: 'Count games with − / + while the match runs: everyone sees the score, Confirm saves it.' },
    { icon: 'pi-users', title: 'Quick team entry', description: 'Paste a list of team names and add them in one go.' },
    { icon: 'pi-chart-bar', title: 'Live standings', description: 'Group tables with wins, game difference and recent form, always up to date.' },
    { icon: 'pi-lock', title: 'Safe workflow', description: 'Every stage follows the tournament rules: no starting twice, a confirmed result is final.' },
  ];

  readonly steps = [
    { number: '01', title: 'Create your organization', description: 'Sign up and get a private workspace, then invite your co-organizers.' },
    { number: '02', title: 'Set up a tournament', description: 'Pick a format and the number of teams and groups, with a live preview.' },
    { number: '03', title: 'Run it live', description: 'Add teams, start, and score with − / +. Everyone sees the score live, Confirm saves it.' },
  ];
}
