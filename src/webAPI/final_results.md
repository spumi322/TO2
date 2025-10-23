●User approved Claude's plan:
  ⎿  Implementation Plan: Store Final Results in TournamentTeam

     Step 4: Create method to collect and store final results

     4.1 Update Domain Entity

     - Add to TournamentTeam entity:
       - FinalPlacement (int?) - Overall tournament placement (1, 2, 3...)
       - EliminatedInRound (int?) - Which bracket round eliminated in (null if champion or groups)
       - ResultFinalizedAt (DateTime?) - Timestamp when placement determined

     4.2 Create Database Migration

     - Generate EF Core migration for new TournamentTeam fields
     - Apply migration

     4.3 Create Placement Calculation Logic

     - Add method to OrchestrationService or TournamentService:
       - CalculateFinalPlacements(tournamentId, bracketStandingId)
       - Algorithm:
           i. Get all bracket matches ordered by Round DESC
         ii. Find max round (final)
         iii. Final match winner = 1st place
         iv. Final match loser = 2nd place
         v. Semi-finals (Round = max-1) losers = 3rd/4th (by seed order)
         vi. Quarter-finals (Round = max-2) losers = 5th-8th
         vii. Continue pattern for all rounds
       - Returns: List of (TeamId, Placement, EliminatedInRound)

     4.4 Create Method to Persist Results

     - Add method: StoreFinalResults(tournamentId, placements)
     - Updates all TournamentTeam records with:
       - FinalPlacement
       - EliminatedInRound
       - ResultFinalizedAt = DateTime.UtcNow
     - Transactional with tournament finish

     4.5 Integrate into Tournament Finish Flow

     - In OrchestrationService.ProcessGameResult():
       - When isFinal = true:
           - After tournament.Status = Finished
         - Call CalculateFinalPlacements()
         - Call StoreFinalResults()
         - Return placements in DTO

     4.6 Update DTOs

     - Add to GameProcessResultDTO:
       - List<FinalStandingDTO>? FinalStandings
     - This sends results to frontend immediately on finish

     Step 5: Update frontend to use new result object

     5.1 Update Frontend Models

     - Add finalStandings to GameProcessResult interface
     - Ensure FinalStanding interface matches backend DTO

     5.2 Update BracketComponent

     - Emit finalStandings when tournament finishes
     - Add to MatchFinishedIds or create new event type

     5.3 Update TournamentDetailsComponent

     - onBracketMatchFinished():
       - When tournamentFinished = true
       - Store finalStandings in component property
       - Trigger UI refresh for Results tab and Top 4 card

     5.4 Update Template Conditions

     - Results tab: Already checks tournament.status
     - Top 4 card: Already uses topFourStandings property
     - Should work automatically once data flows through

     ---
     Testing Points:
     - Verify migration applies cleanly
     - Test placement calculation for 4, 8, 16 team brackets
     - Verify results persist in database
     - Confirm frontend receives and displays data
     - Check Results tab appears after final match
     - Verify Top 4 card shows in Overview

     Expected Outcome:
     When final bracket match finishes, placements are calculated, stored in database, returned to frontend, and immediately displayed in Results tab and Top 4 overview card.
  ⎿