import { Component, computed, input } from '@angular/core';
import { Format, TournamentStatus } from '../../shared/enums';
import { StageCounts, stageFill, StageKey, StageStep, stageSteps, StepState } from './stage-steps';

// Each stage keeps its shade once reached, so the track fills light to dark green
const STAGE_COLORS: Record<StageKey, string> = {
  registration: 'var(--status-setup)',
  groups: 'var(--status-groups)',
  playoffs: 'var(--status-bracket)',
  finished: 'var(--status-finished)',
};

const STATE_WORDS: Record<StepState, string> = {
  done: 'done',
  current: 'in progress',
  ready: 'ready to start',
  todo: 'not started',
};

// Segment bar of the format's stages: done, current, ready (outlined), not started
@Component({
  selector: 'app-stage-track',
  template: `
    <div class="track" role="img" [attr.aria-label]="ariaLabel()">
      @for (step of steps(); track step.key) {
        <div class="step">
          <span class="seg" [class]="step.state" [style.--seg-color]="colors[step.key]">
            <span class="fill" [style.width.%]="fill(step)"></span>
          </span>
          <span class="label" [class.now]="step.state === 'current' || step.state === 'ready'">{{ step.label }}</span>
        </div>
      }
    </div>
  `,
  styles: `
    .track {
      display: flex;
      gap: 5px;
      min-width: 0;
    }
    .step {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 0;
    }
    .seg {
      display: block;
      height: 6px;
      border-radius: 3px;
      background: var(--border);
      overflow: hidden;
    }
    .fill {
      display: block;
      height: 100%;
      border-radius: 3px;
      background: var(--seg-color);
    }
    /* Finished stages fade out, the current one stays strong */
    .done .fill {
      opacity: 0.35;
    }
    .seg.ready {
      background: transparent;
      box-shadow: inset 0 0 0 1.5px var(--status-groups-done);
    }
    .label {
      font-size: 11px;
      line-height: 1.2;
      color: var(--text-muted);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .label.now {
      color: var(--text);
      font-weight: 600;
    }
  `,
})
export class StageTrack {
  readonly format = input.required<Format>();
  readonly status = input.required<TournamentStatus>();
  // Registered teams, finished groups and bracket rounds: segments fill as the stage goes on
  readonly counts = input.required<StageCounts>();

  protected readonly colors = STAGE_COLORS;
  readonly steps = computed(() => stageSteps(this.format(), this.status()));

  protected fill(step: StageStep): number {
    return stageFill(step, this.counts());
  }
  readonly ariaLabel = computed(() => this.steps().map((s) => `${s.label} ${STATE_WORDS[s.state]}`).join(', '));
}
