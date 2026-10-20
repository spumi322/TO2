import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StandingGame, StandingMatch } from '../../../core/models';
import { GameWinnerChange, MatchCard } from './match-card';

const ALPHA = 1;
const BRAVO = 2;

function game(id: number, winnerId: number | null = null): StandingGame {
  return { id, matchId: 50, teamAId: ALPHA, teamAScore: null, teamBId: BRAVO, teamBScore: null, winnerId, version: 3 };
}

function match(games: StandingGame[], winnerId: number | null = null): StandingMatch {
  return {
    id: 50,
    standingId: 9,
    round: 1,
    seed: 1,
    teamAId: ALPHA,
    teamBId: BRAVO,
    winnerId,
    loserId: null,
    bestOf: 3,
    version: 0,
    teamAWins: games.filter((g) => g.winnerId === ALPHA).length,
    teamBWins: games.filter((g) => g.winnerId === BRAVO).length,
    games,
  };
}

describe('MatchCard', () => {
  let fixture: ComponentFixture<MatchCard>;
  let emitted: GameWinnerChange[];

  function render(m: StandingMatch, canScore: boolean): HTMLElement {
    fixture.componentRef.setInput('match', m);
    fixture.componentRef.setInput('teamNames', new Map([[ALPHA, 'Alpha'], [BRAVO, 'Bravo']]));
    fixture.componentRef.setInput('canScore', canScore);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const teamButtons = (el: HTMLElement) => Array.from(el.querySelectorAll<HTMLButtonElement>('button.team'));
  const chips = (el: HTMLElement) => Array.from(el.querySelectorAll<HTMLButtonElement>('button.chip'));

  beforeEach(() => {
    fixture = TestBed.createComponent(MatchCard);
    emitted = [];
    fixture.componentInstance.gameWinner.subscribe((change) => emitted.push(change));
  });

  it('gives the next unplayed game to the tapped team', () => {
    const el = render(match([game(10, ALPHA), game(11), game(12)]), true);

    teamButtons(el)[1].click();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].game.id).toBe(11);
    expect(emitted[0].winnerId).toBe(BRAVO);
  });

  it('is read-only without canScoreMatches', () => {
    const el = render(match([game(10), game(11), game(12)]), false);

    teamButtons(el).forEach((button) => {
      expect(button.disabled).toBe(true);
      button.click();
    });

    expect(emitted).toHaveLength(0);
  });

  it('gives a played game to the other team when its chip is tapped', () => {
    const el = render(match([game(10, ALPHA), game(11, BRAVO), game(12)]), true);

    chips(el)[0].click();

    expect(emitted).toEqual([expect.objectContaining({ winnerId: BRAVO })]);
    expect(emitted[0].game.id).toBe(10);
  });

  it('can not be changed once the match has a winner', () => {
    const el = render(match([game(10, ALPHA), game(11, ALPHA), game(12)], ALPHA), true);

    teamButtons(el)[0].click();

    expect(emitted).toHaveLength(0);
    expect(chips(el)).toHaveLength(0);
    expect(el.querySelector('.team.won')?.textContent).toContain('Alpha');
  });
});
