import { Component, effect, ElementRef, input, output, signal, untracked, viewChild } from '@angular/core';
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
      onMatchClick: (match: { id: number | string }) => void;
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

export interface BracketMatchClick {
  matchId: number;
  // The clicked match element, to anchor a popover
  element: HTMLElement | null;
}

/**
 * The only place that touches brackets-viewer: data goes through the pure adapter,
 * the library renders it, clicks come back as match ids. Re-renders whenever an input changes.
 */
@Component({
  selector: 'app-bracket-view',
  template: `
    @if (failed()) {
      <p class="form-error">The bracket could not be displayed. Reload the page to try again.</p>
    }
    <div #host class="brackets-viewer" [id]="elementId" [class.clickable]="canScore()"></div>
  `,
  styleUrl: './bracket-view.css',
})
export class BracketView {
  readonly bracket = input.required<Bracket>();
  readonly teamNames = input.required<Map<number, string>>();
  readonly tournamentId = input.required<number>();
  readonly canScore = input(false);
  readonly matchClick = output<BracketMatchClick>();

  protected readonly elementId = `bracket-view-${++nextId}`;
  protected readonly failed = signal(false);
  private readonly host = viewChild.required<ElementRef<HTMLElement>>('host');

  constructor() {
    effect(() => {
      const data = toViewerData(this.bracket(), this.teamNames(), this.tournamentId());
      untracked(() => void this.render(data));
    });
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
        onMatchClick: (match) => this.onMatchClick(Number(match.id)),
        customRoundName: (info) => (info.fractionOfFinal ? roundName(info.fractionOfFinal) : ''),
      });
      this.failed.set(false);
    } catch (error) {
      console.error(error);
      this.failed.set(true);
    }
  }

  private onMatchClick(matchId: number): void {
    if (!this.canScore()) return;
    const element = this.host().nativeElement.querySelector<HTMLElement>(`[data-match-id="${matchId}"]`);
    this.matchClick.emit({ matchId, element });
  }
}
