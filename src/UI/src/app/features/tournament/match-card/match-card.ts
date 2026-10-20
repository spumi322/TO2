import { Component, computed, input, output } from '@angular/core';
import { StandingGame, StandingMatch } from '../../../core/models';

export interface GameWinnerChange {
  match: StandingMatch;
  game: StandingGame;
  winnerId: number;
}

/**
 * One match (groups and bracket). Scoring is winner-only:
 * a team's +1 button gives it the next unplayed game; a played game's chip gives that game to the other team.
 * Both work until the match has a winner (the backend allows re-scoring a game until then).
 */
@Component({
  selector: 'app-match-card',
  templateUrl: './match-card.html',
  styleUrl: './match-card.css',
})
export class MatchCard {
  readonly match = input.required<StandingMatch>();
  readonly teamNames = input.required<Map<number, string>>();
  readonly canScore = input(false);
  readonly busy = input(false);
  readonly gameWinner = output<GameWinnerChange>();

  readonly teamA = computed(() => this.side(this.match().teamAId, this.match().teamAWins));
  readonly teamB = computed(() => this.side(this.match().teamBId, this.match().teamBWins));
  readonly games = computed(() => [...this.match().games].sort((a, b) => a.id - b.id));
  readonly playedGames = computed(() => this.games().filter((game) => game.winnerId !== null));
  readonly nextGame = computed(() => this.games().find((game) => game.winnerId === null) ?? null);
  readonly decided = computed(() => this.match().winnerId !== null);
  readonly live = computed(() => !this.decided() && this.playedGames().length > 0);

  // Both teams known (0/null = not decided yet) and no winner yet
  readonly scorable = computed(
    () => this.canScore() && !this.decided() && !!this.match().teamAId && !!this.match().teamBId,
  );

  scoreNext(teamId: number | null): void {
    const game = this.nextGame();
    if (!this.scorable() || this.busy() || !game || !teamId) return;
    this.gameWinner.emit({ match: this.match(), game, winnerId: teamId });
  }

  // Corrects a played game: its winner becomes the other team
  flip(game: StandingGame): void {
    const { teamAId, teamBId } = this.match();
    if (!this.scorable() || this.busy() || !teamAId || !teamBId) return;
    this.gameWinner.emit({ match: this.match(), game, winnerId: game.winnerId === teamAId ? teamBId : teamAId });
  }

  gameLabel(game: StandingGame): string {
    return this.teamNames().get(game.winnerId ?? 0) ?? '?';
  }

  private side(teamId: number | null, wins: number) {
    const known = !!teamId;
    const winnerId = this.match().winnerId;
    return {
      id: teamId,
      name: known ? (this.teamNames().get(teamId!) ?? 'Unknown team') : 'TBD',
      known,
      wins,
      won: known && winnerId === teamId,
      lost: known && winnerId !== null && winnerId !== teamId,
    };
  }
}
