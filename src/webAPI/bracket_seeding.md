 How It Displays Brackets

  The viewer needs this data structure:
  {
    stages: [{id, tournament_id, name, type: 'single_elimination'}],
    matches: [
      {
        id: 1,
        round_id: 1,
        number: 1,  // Position in round (seed)
        opponent1: {id: 5, score: 0} or null,  // null = TBD
        opponent2: {id: 8, score: 0} or null
      },
      // ... ALL matches in ALL rounds
    ],
    participants: [{id, name}]
  }

  Critical: The library renders based on the complete structure you provide. If you only give Round 1 matches → it only renders Round 1. To show a full bracket tree → you must provide ALL rounds.

  ---
  🎯 Backend Recommendation: Generate ALL Matches Upfront

  Option 1: Generate ALL Matches in SeedingBracket ✅ RECOMMENDED

  When: During GroupsCompleted → SeedingBracket transition

  What to Generate:

  For 8 teams (example):
  Round 1: 4 matches (all with real teams)
    Match 1: Team A vs Team B
    Match 2: Team C vs Team D
    Match 3: Team E vs Team F
    Match 4: Team G vs Team H

  Round 2: 2 matches (TBD teams)
    Match 5: Winner(M1) vs Winner(M2)  ← TeamAId = NULL, TeamBId = NULL
    Match 6: Winner(M3) vs Winner(M4)

  Round 3: 1 match (TBD teams)
    Match 7: Winner(M5) vs Winner(M6)

  Database Schema:
  Match table:
  - Id
  - StandingId
  - Round (1, 2, 3)
  - Seed (position in round)
  - TeamAId (NULL for TBD)
  - TeamBId (NULL for TBD)
  - WinnerId (NULL until complete)
  - LoserId (NULL until complete)
  - ParentMatchId (which match does winner advance to)  ← NEW FIELD NEEDED

  Advantages:
  ✅ Viewer displays full bracket tree immediately✅ Simple logic: one-time generation✅ Easy to understand state✅ Matches how brackets-manager.js works✅ Your adapter already expects this structure!

  Disadvantages:
  ❌ Stores TBD matches in database❌ More rows (but negligible - max 64 matches for 32 teams)

  ---
  Option 2: Generate Matches Dynamically ❌ NOT RECOMMENDED

  When: Create next round's matches only after previous round completes

  Advantages:
  ✅ Fewer database rows initially✅ No NULL teams

  Disadvantages:
  ❌ Viewer can't display full bracket tree❌ Complex logic: need to detect round completion❌ Race conditions during concurrent match updates❌ Your adapter would need major rewrite❌ Doesn't work with how bracket-viewer expects data

  ---
  🔄 Recommended Tournament Lifecycle Flow

  Current State: GroupsCompleted (Status = 4)

  User clicks "Start Bracket" button

  Backend: Start Bracket Endpoint

  POST /api/tournaments/{id}/start-bracket

  Process:
  1. Validate tournament status = GroupsCompleted
  2. Transition: GroupsCompleted → SeedingBracket (Status 4 → 5)
  3. Get top teams from groups (already implemented in your StandingService)
  4. Generate ALL bracket matches:
    - Calculate total rounds: log2(teamCount)
    - Round 1: Create matches with actual team assignments
    - Round 2-N: Create matches with TeamAId = NULL, TeamBId = NULL
    - Set ParentMatchId relationships (winner of M1 goes to M5, etc.)
  5. Transition: SeedingBracket → BracketInProgress (Status 5 → 6)
  6. Save all matches + update tournament status
  7. Return success response

  Response:
  {
    "success": true,
    "message": "Bracket seeded with 8 teams",
    "tournamentStatus": 6
  }

  Frontend: Receives Response

  - Reloads tournament data
  - Bracket tab now shows full bracket tree
  - Round 1 matches have real teams, others show "TBD"
  - Users can click Round 1 matches to score them

  ---
  🏗️ What Your Backend Needs to Handle

  1. Bracket Seeding Algorithm

  Single Elimination Seeding (prevents top teams meeting early):
  // For 8 teams ranked 1-8:
  Round 1 matchups:
    Match 1: Rank 1 vs Rank 8
    Match 2: Rank 4 vs Rank 5
    Match 3: Rank 2 vs Rank 7
    Match 4: Rank 3 vs Rank 6

  Standard seeding ensures: #1 can't meet #2 until finals

  2. Match Progression Logic

  When a match completes:
  // User scores Match 1, Team A wins
  1. Set Match.WinnerId = TeamA.Id
  2. Set Match.LoserId = TeamB.Id
  3. Find ParentMatch (Match 5 in this case)
  4. Update ParentMatch:
     - If Match 1's winner goes to opponent1: ParentMatch.TeamAId = TeamA.Id
     - Otherwise: ParentMatch.TeamBId = TeamA.Id
  5. Check if ParentMatch now has both teams assigned
  6. If both teams assigned AND it's the final match → tournament finished

  3. Database Schema Addition

  You need to track match relationships:

  Option A: ParentMatchId approach
  public class Match
  {
      // Existing fields...
      public long? ParentMatchId { get; set; }  // Which match winner advances to
      public int? ParentSlot { get; set; }      // 1 or 2 (opponent1 or opponent2)
  }

  Option B: ChildMatch approach (alternative)
  public class Match
  {
      public long? ChildMatchAId { get; set; }  // Match whose winner fills TeamAId
      public long? ChildMatchBId { get; set; }  // Match whose winner fills TeamBId
  }

  ---
  📊 Data Flow Summary

  GroupsCompleted → User clicks "Start Bracket"
                       ↓
                SeedingBracket (transition state)
                       ↓
           Generate ALL bracket matches:
           - Round 1: Real teams
           - Round 2-N: NULL teams (TBD)
           - Set parent-child relationships
                       ↓
                BracketInProgress
                       ↓
           Frontend loads matches
           brackets-viewer renders full tree
                       ↓
           User scores matches one by one
           Winners auto-populate next round
                       ↓
           Final match completed
                       ↓
                Tournament → Finished

  ---
  🎨 What brackets-viewer Provides

  Features:
  ✅ Automatic tree layout (rounds, connections, positioning)✅ Responsive design✅ Shows match status (locked/ready/completed)✅ Displays scores✅ Click handlers for matches✅ TBD team support

  What it DOESN'T do:
  ❌ Match progression logic (your backend does this)❌ Seeding algorithm (your backend does this)❌ State management (your backend does this)

  ---
  💡 My Recommendation

  Generate ALL matches during SeedingBracket transition because:

  1. Your BracketAdapterService already expects this structure (lines 64-108)
  2. Viewer needs full bracket tree to render properly
  3. Simpler backend logic
  4. Standard approach used by brackets-manager.js
  5. Easy to visualize and debug

  Would you like me to create a plan for implementing the backend bracket generation logic following this approach?