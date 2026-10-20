import { Component, effect, input, output, signal, untracked } from '@angular/core';
import { Bracket } from '../../../core/models';
import { roundName, toViewerData, ViewerData } from './bracket-adapter';

// The part of the brackets-viewer global this wrapper uses
interface BracketsViewerApi {
  render(
    data: ViewerData,
    config: {
      selector: string;
      clear: boolean;
      participantOriginPlacement: 'none';
      showSlotsOrigin: boolean;
      separatedChildCountLabel: boolean;
      customRoundName: (info: { fractionOfFinal?: number }) => string;
    },
  ): Promise<void>;
}

declare global {
  interface Window {
    bracketsViewer?: BracketsViewerApi;
  }
}

// The library is a browser global script (copied to brackets-viewer/ by angular.json): loaded once, on first use
let viewerLoading: Promise<BracketsViewerApi> | null = null;

function loadViewer(): Promise<BracketsViewerApi> {
  viewerLoading ??= new Promise<void>((resolve, reject) => {
    const style = document.createElement('link');
    style.rel = 'stylesheet';
    style.href = 'brackets-viewer/brackets-viewer.min.css';
    document.head.append(style);

    const script = document.createElement('script');
    script.src = 'brackets-viewer/brackets-viewer.min.js';
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('brackets-viewer failed to load'));
    document.head.append(script);
  }).then(() => window.bracketsViewer!);
  return viewerLoading;
}

let nextId = 0;

export interface BracketTeamClick {
  matchId: number;
  teamId: number;
}

/**
 * The only place that touches brackets-viewer: data goes through the pure adapter, the library renders it,
 * and one click listener on the container turns a click on a team into (match id, team id).
 * Re-renders whenever an input changes.
 */
@Component({
  selector: 'app-bracket-view',
  template: `
    @if (failed()) {
      <p class="form-error">The bracket could not be displayed. Reload the page to try again.</p>
    }
    <div class="brackets-viewer" [id]="elementId" [class.clickable]="canScore()" (click)="onClick($event)"></div>
  `,
  styleUrl: './bracket-view.css',
})
export class BracketView {
  readonly bracket = input.required<Bracket>();
  readonly teamNames = input.required<Map<number, string>>();
  readonly tournamentId = input.required<number>();
  readonly canScore = input(false);
  readonly teamClick = output<BracketTeamClick>();

  protected readonly elementId = `bracket-view-${++nextId}`;
  protected readonly failed = signal(false);

  constructor() {
    effect(() => {
      const data = toViewerData(this.bracket(), this.teamNames(), this.tournamentId());
      untracked(() => void this.render(data));
    });
  }

  // The library marks matches with data-match-id and teams with data-participant-id
  protected onClick(event: MouseEvent): void {
    if (!this.canScore() || !(event.target instanceof Element)) return;
    const team = event.target.closest<HTMLElement>('[data-participant-id]');
    const match = team?.closest<HTMLElement>('[data-match-id]');
    if (!team || !match) return;
    this.teamClick.emit({ matchId: Number(match.dataset['matchId']), teamId: Number(team.dataset['participantId']) });
  }

  private async render(data: ViewerData): Promise<void> {
    try {
      const viewer = await loadViewer();
      await viewer.render(data, {
        selector: `#${this.elementId}`,
        clear: true,
        participantOriginPlacement: 'none',
        showSlotsOrigin: false,
        separatedChildCountLabel: true,
        customRoundName: (info) => (info.fractionOfFinal ? roundName(info.fractionOfFinal) : ''),
      });
      this.failed.set(false);
    } catch (error) {
      console.error(error);
      this.failed.set(true);
    }
  }
}
