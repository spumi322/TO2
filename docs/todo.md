1. Validations (team name)
2. Tests
3. Auth


Optional:
1. Caching
2. Optimization(DB, fuctions)
3. Backend refactor(dependencies of the services, clean up endpoints)
4. Deploy
5. Tie
6. Score edit 
7. Scheduling
8. Stats



Create Tournament -> Add/Remove Teams -> Start Tournament(change to start groups?) | BETWEEN STATE UNTIL GROUPS ARE PROPERLY SEEDED | -> Play matches in groups -> All Groups Finished -> *I NEED A NEW STEP HERE (start bracket maybe) | ITS A BETWEEN STATE TOO, WHERE BRACKET ARE SEEDED| -> Play Bracket Matches(WIP, not important now) -> Declare Champion -> Tournament Finished


G:\Code\TO2\src\Application\Services\OrchestrationService.cs.bak ????????????????

Problem: Related objects dont show up, system uses more API calls and db queries, then it should. Not using EFcore to full potential.
Solution: Refactor the Repository and use Unit Of Work for complex flows, consider dropping wrapper around dbcontext.

I see - GetAllByFK() does NOT include navigation properties (line 42-47). So when backend returns Match[], the games collection is empty!

  That's why the matches component makes separate API calls for games. Let me verify how bracket component loads matches:

● Read(G:\Code\TO2\src\UI\src\app\components\standing\bracket\bracket.component.ts)
  ⎿  Read 110 lines

● Perfect! Now I see the issue:

  Current Problem:

  bracket.component.ts:52-54 loads matches:
  this.matchService.getMatchesByStandingId(this.standingId) // Returns Match[] WITHOUT games

  bracket-adapter.service.ts:57-61 tries to calculate scores:
  const calculateScore = (match: Match, teamId: number): number => {
    if (!match.games || match.games.length === 0) {  // Always true - games is empty!
      return 0;
    }
    return match.games.filter(game => game.winnerId === teamId).length;
  };

  Result: All scores show as 0-0 because match.games is empty!

  How Groups Handle This:

  matches.component.ts:33-39 loads games separately for each match:
  this.matches.forEach(match => {
    this.matchService.getAllGamesByMatch(match.id).subscribe(games => {
      match.result = this.getMatchResults(games); // Calculate scores from games
    });
  });


