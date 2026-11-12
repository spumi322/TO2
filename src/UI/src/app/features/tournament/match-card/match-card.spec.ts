import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatchDraft, StandingMatch } from '../../../core/models';
import { DraftChange, MatchCard } from './match-card';

const ALPHA = 1;
const BRAVO = 2;

function match(overrides: Partial<StandingMatch> = {}): StandingMatch {
  return {
    id: 50,
    standingId: 9,
    round: 1,
    seed: 1,
    teamAId: ALPHA,
    teamBId: BRAVO,
    winnerId: null,
    loserId: null,
    bestOf: 3,
    version: 0,
    teamAWins: 0,
    teamBWins: 0,
    games: [],
    ...overrides,
  };
}

describe('MatchCard', () => {
  let fixture: ComponentFixture<MatchCard>;
  let changes: DraftChange[];
  let confirmed: StandingMatch[];

  function render(m: StandingMatch, canScore: boolean, draft: MatchDraft | null = null): HTMLElement {
    fixture.componentRef.setInput('match', m);
    fixture.componentRef.setInput('teamNames', new Map([[ALPHA, 'Alpha'], [BRAVO, 'Bravo']]));
    fixture.componentRef.setInput('canScore', canScore);
    fixture.componentRef.setInput('draft', draft);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const button = (el: HTMLElement, label: string) => el.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
  const confirmButton = (el: HTMLElement) => el.querySelector<HTMLButtonElement>('button.confirm');

  beforeEach(() => {
    fixture = TestBed.createComponent(MatchCard);
    changes = [];
    confirmed = [];
    fixture.componentInstance.draftChange.subscribe((change) => changes.push(change));
    fixture.componentInstance.confirm.subscribe((m) => confirmed.push(m));
  });

  it('+ gives a team a game in the draft', () => {
    const el = render(match(), true, { teamAWins: 1, teamBWins: 0, by: null });

    button(el, 'Give Bravo a game')!.click();

    expect(changes).toEqual([expect.objectContaining({ teamAWins: 1, teamBWins: 1 })]);
  });

  it('− takes a game back, never below 0', () => {
    const el = render(match(), true, { teamAWins: 1, teamBWins: 0, by: null });

    expect(button(el, 'Take a game from Bravo')!.disabled).toBe(true);
    button(el, 'Take a game from Alpha')!.click();

    expect(changes).toEqual([expect.objectContaining({ teamAWins: 0, teamBWins: 0 })]);
  });

  it('shows Confirm when a team has the wins needed, and no more +', () => {
    const el = render(match(), true, { teamAWins: 2, teamBWins: 1, by: null });

    expect(button(el, 'Give Alpha a game')!.disabled).toBe(true);
    expect(button(el, 'Give Bravo a game')!.disabled).toBe(true);
    confirmButton(el)!.click();

    expect(confirmed).toHaveLength(1);
  });

  it('needs 3 wins in a best of 5', () => {
    const el = render(match({ bestOf: 5 }), true, { teamAWins: 2, teamBWins: 0, by: null });

    expect(confirmButton(el)).toBeNull();
    expect(button(el, 'Give Alpha a game')!.disabled).toBe(false);
  });

  it('labels a draft someone else is entering', () => {
    const el = render(match(), true, { teamAWins: 1, teamBWins: 1, by: 'anna' });

    expect(el.textContent).toContain('draft · anna');
  });

  it('is read-only without canScoreMatches', () => {
    const el = render(match(), false, { teamAWins: 1, teamBWins: 0, by: 'anna' });

    expect(el.querySelectorAll('button')).toHaveLength(0);
    expect(el.textContent).toContain('draft · anna');
  });

  it('shows only the score once confirmed', () => {
    const el = render(match({ winnerId: ALPHA, loserId: BRAVO, teamAWins: 2, teamBWins: 1 }), true);

    expect(el.querySelectorAll('button')).toHaveLength(0);
    expect(el.querySelector('.name.won')?.textContent).toContain('Alpha');
    expect(el.textContent).toContain('Confirmed');
  });
});
