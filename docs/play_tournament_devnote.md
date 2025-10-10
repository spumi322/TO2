# Play Tournament - Implementation Plan

## Overview

This document outlines the step-by-step implementation of the "Play Tournament" feature. The feature allows tournaments to progress from group stage through bracket elimination to declaring a champion.

**Current State:**
- ✅ Create Tournament
- ✅ Add/Remove Teams  
- ✅ Start Tournament (generates groups and matches)

**Goal:**
- ✅ Play matches and update standings
- ✅ Auto-advance teams from groups to bracket
- ✅ Progress through bracket rounds
- ✅ Declare champion

**Implementation Approach:**
Each phase is self-contained with backend + frontend + testing. Complete one phase, test manually, commit to git, then proceed to next phase.

---

## PHASE 1: Group Standings Auto-Update

**Goal:** When a match finishes, group standings (Wins/Losses/Points) update automatically.

### Backend Changes

**File:** `src/Application/Services/GameService.cs`

**Location:** Find the `UpdateStandingAfterMatch` method (currently commented out)

**Action:** Uncomment and implement this method:

```csharp
public async Task UpdateStandingAfterMatch(Match match)
{
    var standing = await _standingrepository.Get(match.StandingId)
        ?? throw new Exception("Standing not found");

    // Only update if it's a Group standing
    if (standing.StandingType == StandingType.Group)
    {
        var teamA = await _dbContext.GroupEntries
            .FirstOrDefaultAsync(g => g.TeamId == match.TeamAId && g.StandingId == standing.Id);

        var teamB = await _dbContext.GroupEntries
            .FirstOrDefaultAsync(g => g.TeamId == match.TeamBId && g.StandingId == standing.Id);

        if (teamA == null || teamB == null) 
            throw new Exception("Teams not found in group");

        if (match.WinnerId == teamA.TeamId)
        {
            teamA.Wins += 1;
            teamA.Points += 3;
            teamB.Losses += 1;
        }
        else if (match.WinnerId == teamB.TeamId)
        {
            teamB.Wins += 1;
            teamB.Points += 3;
            teamA.Losses += 1;
        }

        await _dbContext.SaveChangesAsync();
    }
}
```

### Frontend Changes

**File:** `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.ts`

**Action:** Add match finished handler:

```typescript
handleMatchFinished(result: MatchFinishedIds): void {
  // Reload tournament data to refresh all standings
  this.reloadTournamentData();
  this.showSuccess('Match finished! Standings updated.');
}
```

**File:** `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.html`

**Action:** Connect the event:

```html
<app-group *ngIf="selectedTab === 'groups'" 
           [groups]="groups"
           (matchFinished)="handleMatchFinished($event)">
</app-group>
```

### Testing Checklist

- [ ] Start a tournament with 4 groups
- [ ] Click + button to give Team A a game win
- [ ] Verify match score updates (e.g., 1-0)
- [ ] Complete the match (Bo3 = 2 games won)
- [ ] **Verify:** Winner gets +1 Win, +3 Points
- [ ] **Verify:** Loser gets +1 Loss  
- [ ] **Verify:** Standings table updates and re-sorts
- [ ] Play another match in the same group
- [ ] **Verify:** Standings update correctly again
- [ ] Test with Bo1 and Bo5 formats

### Commit Message
```
feat: implement automatic group standings updates after matches
```

---

## PHASE 2: Detect Individual Group Completion

**Goal:** System detects when a single group finishes all matches and marks it as complete.

### Backend Changes

**File:** `src/Application/Contracts/IStandingService.cs`

**Action:** Add method signature:

```csharp
Task CheckAndMarkStandingAsFinishedAsync(long tournamentId);
```

**File:** `src/Application/Services/StandingService.cs`

**Action:** Implement the method:

```csharp
public async Task CheckAndMarkStandingAsFinishedAsync(long tournamentId)
{
    var standings = await GetStandingsAsync(tournamentId);
    
    foreach (var standing in standings.Where(s => s.StandingType == StandingType.Group && !s.IsFinished))
    {
        var matches = await _matchRepository.GetAllByFK("StandingId", standing.Id);
        
        // Check if all matches in this group have winners
        if (matches.Any() && matches.All(m => m.WinnerId.HasValue))
        {
            standing.IsFinished = true;
            standing.CanSetMatchScore = false;
            await _standingRepository.Update(standing);
            await _standingRepository.Save();
            
            _logger.LogInformation($"Group {standing.Name} (ID: {standing.Id}) marked as finished");
        }
    }
}
```

**File:** `src/Application/Services/GameService.cs`

**Action:** Call this method in `SetGameResult` after determining match winner:

```csharp
public async Task<MatchResultDTO?> SetGameResult(long gameId, SetGameResultDTO request)
{
    // ... existing code ...
    
    if (result is not null)
    {
        var standing = await _standingrepository.Get(match.StandingId);

        // Check if this match completion finished the group
        await _standingService.CheckAndMarkStandingAsFinishedAsync(standing.TournamentId);

        return new MatchResultDTO(result.WinnerId, result.LoserId);
    }
    
    // ... rest of code ...
}
```

### Frontend Changes

**File:** `src/UI/src/app/components/standing/group/group.component.html`

**Action:** Add status badges to group headers:

```html
<div class="group-card" *ngFor="let group of groups">
  <div class="group-header">
    {{ group.name }}
    <span *ngIf="group.isFinished" class="status-badge finished">
      <i class="pi pi-check-circle"></i> Completed
    </span>
    <span *ngIf="!group.isFinished" class="status-badge active">
      <i class="pi pi-spin pi-spinner"></i> In Progress
    </span>
  </div>
  
  <!-- Rest of template -->
</div>
```

**File:** `src/UI/src/app/components/standing/group/group.component.css`

**Action:** Add styles:

```css
.group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px;
  background: #f8f9fa;
  border-bottom: 2px solid #dee2e6;
}

.status-badge {
  padding: 5px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
}

.status-badge.finished {
  background: #d4edda;
  color: #155724;
}

.status-badge.active {
  background: #fff3cd;
  color: #856404;
}
```

**File:** `src/UI/src/app/components/matches/matches.component.html`

**Action:** Disable buttons for finished groups:

```html
<button pButton
        type="button"
        icon="pi pi-plus"
        class="p-button-rounded p-button-primary"
        (click)="updateMatchScore(match.id, match.teamAId, match.result.teamAWins + 1, match.result.teamBWins)"
        [disabled]="match.winnerId || isUpdating[match.id] || isGroupFinished">
</button>
```

**File:** `src/UI/src/app/components/matches/matches.component.ts`

**Action:** Add input property:

```typescript
@Input() isGroupFinished: boolean = false;
```

**File:** `src/UI/src/app/components/standing/group/group.component.html`

**Action:** Pass the property:

```html
<app-matches [matches]="group.matches"
             [teams]="group.teams"
             [isGroupFinished]="group.isFinished"
             (matchFinished)="onMatchFinished($event)">
</app-matches>
```

### Testing Checklist

- [ ] Start tournament with 4 groups
- [ ] Complete all but one match in Group 1
- [ ] **Verify:** Group 1 shows "In Progress" badge
- [ ] Complete the last match in Group 1
- [ ] **Verify:** Group 1 shows "Completed" badge
- [ ] **Verify:** Match buttons in Group 1 are disabled
- [ ] **Verify:** Groups 2, 3, 4 still show "In Progress"
- [ ] **Verify:** Can still play matches in other groups
- [ ] Check database: `IsFinished = true` for Group 1
- [ ] Check database: `CanSetMatchScore = false` for Group 1

### Commit Message
```
feat: detect and mark individual groups as finished
```

---

## PHASE 3: Advance Teams to Bracket

**Goal:** When all groups finish, calculate which teams advance to bracket and automatically seed bracket matches.

### Backend Changes

**File:** `src/Application/Contracts/IStandingService.cs`

**Action:** Add method signatures:

```csharp
Task CheckAndMarkAllGroupsAreFinishedAsync(long tournamentId);
Task<List<BracketSeedDTO>> PrepareTeamsForBracket(long tournamentId);
```

**File:** `src/Application/Services/StandingService.cs`

**Action:** Implement both methods:

```csharp
public async Task CheckAndMarkAllGroupsAreFinishedAsync(long tournamentId)
{
    var tournament = await _tournamentRepository.Get(tournamentId);
    var standings = await GetStandingsAsync(tournamentId);
    var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
    
    // If no groups exist (Bracket Only format), skip
    if (!groups.Any())
        return;
    
    // Check if all groups are finished
    if (groups.All(g => g.IsFinished))
    {
        tournament.GroupsFinished = true;
        await _tournamentRepository.Update(tournament);
        await _tournamentRepository.Save();
        
        _logger.LogInformation($"All groups finished for tournament {tournamentId}. Preparing bracket...");
        
        // Automatically prepare teams for bracket
        var advancingTeams = await PrepareTeamsForBracket(tournamentId);
        
        // Seed the bracket
        await _matchService.SeedBracket(tournamentId, advancingTeams);
    }
}

public async Task<List<BracketSeedDTO>> PrepareTeamsForBracket(long tournamentId)
{
    var tournament = await _tournamentRepository.Get(tournamentId);
    var standings = await GetStandingsAsync(tournamentId);
    var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
    var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);
    
    if (bracket == null)
        throw new Exception("Bracket standing not found");
    
    if (groups.Count == 0)
        throw new Exception("No groups found");
    
    // Calculate how many teams advance per group
    // Formula: TeamsPerBracket / NumberOfGroups
    int teamsAdvancingPerGroup = tournament.TeamsPerBracket / groups.Count;
    
    _logger.LogInformation($"Teams advancing per group: {teamsAdvancingPerGroup} (Bracket: {tournament.TeamsPerBracket}, Groups: {groups.Count})");
    
    var advancingTeams = new List<BracketSeedDTO>();
    
    foreach (var group in groups)
    {
        // Get group entries sorted by Points DESC, then Wins DESC
        var groupEntries = await _dbContext.GroupEntries
            .Where(g => g.StandingId == group.Id)
            .OrderByDescending(g => g.Points)
            .ThenByDescending(g => g.Wins)
            .ThenBy(g => g.Losses)
            .ToListAsync();
        
        // Top X teams advance
        var advancing = groupEntries.Take(teamsAdvancingPerGroup).ToList();
        var eliminated = groupEntries.Skip(teamsAdvancingPerGroup).ToList();
        
        int placement = 1;
        foreach (var team in advancing)
        {
            team.Status = TeamStatus.Advanced;
            advancingTeams.Add(new BracketSeedDTO 
            { 
                TeamId = team.TeamId, 
                GroupId = group.Id,
                Placement = placement++
            });
            
            _logger.LogInformation($"Team {team.TeamName} advanced from {group.Name} (Placement: {placement - 1})");
        }
        
        foreach (var team in eliminated)
        {
            team.Status = TeamStatus.Eliminated;
            team.Eliminated = true;
            
            _logger.LogInformation($"Team {team.TeamName} eliminated from {group.Name}");
        }
    }
    
    await _dbContext.SaveChangesAsync();
    
    return advancingTeams;
}
```

**File:** `src/Application/Services/GameService.cs`

**Action:** Add call to check all groups finished:

```csharp
public async Task<MatchResultDTO?> SetGameResult(long gameId, SetGameResultDTO request)
{
    // ... existing code ...
    
    if (result is not null)
    {
        var standing = await _standingrepository.Get(match.StandingId);

        await _standingService.CheckAndMarkStandingAsFinishedAsync(standing.TournamentId);
        
        // NEW: Check if all groups are now finished
        await _standingService.CheckAndMarkAllGroupsAreFinishedAsync(standing.TournamentId);

        return new MatchResultDTO(result.WinnerId, result.LoserId);
    }
    
    // ... rest of code ...
}
```

**File:** `src/Application/Services/MatchService.cs`

**Action:** Update `SeedBracket` method to create BracketEntry records. Find the section after match generation and before the final save, add:

```csharp
// Create BracketEntry records for all advancing teams
foreach (var advancedTeam in advancedTeams)
{
    var team = await _dbContext.Teams.FindAsync(advancedTeam.TeamId);
    if (team != null)
    {
        var bracketEntry = new Bracket(tournamentId, bracket.Id, team);
        bracketEntry.Status = TeamStatus.Competing;
        
        await _dbContext.BracketEntries.AddAsync(bracketEntry);
    }
}
```

### Frontend Changes

**File:** `src/UI/src/app/models/team.ts`

**Action:** Add status field if not present:

```typescript
export interface Team {
  id: number;
  name: string;
  wins: number;
  losses: number;
  points: number;
  status?: number; // 0=SignedUp, 1=Competing, 2=Advanced, 3=Eliminated, 4=Champion
}
```

**File:** `src/UI/src/app/components/standing/group/group.component.html`

**Action:** Add advancement badges to team rows:

```html
<tr *ngFor="let team of group.teams; let i = index"
    [ngClass]="{
      'team-advanced': team.status === 2,
      'team-eliminated': team.status === 3
    }">
  <td class="team-rank">{{ i + 1 }}</td>
  <td class="team-name">
    {{ team.name }}
    <span *ngIf="team.status === 2" class="badge-advanced">✓ Advanced</span>
    <span *ngIf="team.status === 3" class="badge-eliminated">✗ Eliminated</span>
  </td>
  <td>{{ team.wins }}</td>
  <td>{{ team.losses }}</td>
  <td>{{ team.points }}</td>
</tr>
```

**File:** `src/UI/src/app/components/standing/group/group.component.css`

**Action:** Add styles:

```css
.team-advanced {
  background-color: #d4edda !important;
}

.team-eliminated {
  background-color: #f8d7da !important;
  opacity: 0.7;
}

.badge-advanced {
  background: #28a745;
  color: white;
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 10px;
  margin-left: 8px;
}

.badge-eliminated {
  background: #dc3545;
  color: white;
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 10px;
  margin-left: 8px;
}
```

### Testing Checklist

**Test Case 1: 16 Teams, 4 Groups, 8-Team Bracket**
- [ ] Create tournament: MaxTeams=16, TeamsPerGroup=4, TeamsPerBracket=8
- [ ] Add 16 teams
- [ ] Start tournament
- [ ] **Verify:** 4 groups with 4 teams each
- [ ] Complete all matches in Group 1
- [ ] Complete all matches in Group 2
- [ ] Complete all matches in Group 3
- [ ] Complete all matches in Group 4 (last group)
- [ ] **Verify:** After last match, system auto-seeds bracket
- [ ] **Verify:** Top 2 teams from each group show "✓ Advanced"
- [ ] **Verify:** Bottom 2 teams show "✗ Eliminated"
- [ ] Navigate to Bracket tab
- [ ] **Verify:** 4 matches showing (8 teams, Round 1)
- [ ] Check database: `BracketEntries` table has 8 records
- [ ] Check database: `Tournament.GroupsFinished = true`

**Test Case 2: Different Configuration**
- [ ] Create tournament: MaxTeams=12, TeamsPerGroup=4, TeamsPerBracket=6
- [ ] Add 12 teams (3 groups of 4)
- [ ] Complete all groups
- [ ] **Verify:** Top 2 from each group = 6 teams advance
- [ ] **Verify:** Bracket has 3 matches (6 teams need byes for standard bracket)

### Commit Message
```
feat: auto-advance teams from groups to bracket
```

---

## PHASE 4: Bracket Progression

**Goal:** Winners automatically advance to next bracket round until champion is determined.

### Backend Changes

**File:** `src/Application/Services/GameService.cs`

**Action:** Add bracket handling to `UpdateStandingAfterMatch`:

```csharp
public async Task UpdateStandingAfterMatch(Match match)
{
    var standing = await _standingrepository.Get(match.StandingId)
        ?? throw new Exception("Standing not found");

    if (standing.StandingType == StandingType.Group)
    {
        // ... existing group logic ...
    }
    else if (standing.StandingType == StandingType.Bracket)
    {
        // Update bracket entry statuses
        if (match.WinnerId.HasValue && match.LoserId.HasValue)
        {
            var loserEntry = await _dbContext.BracketEntries
                .FirstOrDefaultAsync(b => b.TeamId == match.LoserId && b.StandingId == standing.Id);
            
            if (loserEntry != null)
            {
                loserEntry.Status = TeamStatus.Eliminated;
                loserEntry.Eliminated = true;
            }
            
            await _dbContext.SaveChangesAsync();
            
            // Check if round is complete and generate next round
            await _matchService.CheckAndGenerateNextRound(standing.Id, match.Round);
        }
    }
}
```

**File:** `src/Application/Contracts/IMatchService.cs`

**Action:** Add method signature:

```csharp
Task CheckAndGenerateNextRound(long standingId, int currentRound);
```

**File:** `src/Application/Services/MatchService.cs`

**Action:** Implement next round generation:

```csharp
public async Task CheckAndGenerateNextRound(long standingId, int currentRound)
{
    var matches = await _matchRepository.GetAllByFK("StandingId", standingId);
    var currentRoundMatches = matches.Where(m => m.Round == currentRound).ToList();
    
    // Check if all matches in current round are finished
    if (!currentRoundMatches.All(m => m.WinnerId.HasValue))
    {
        _logger.LogInformation($"Round {currentRound} not yet complete. Waiting for remaining matches.");
        return;
    }
    
    _logger.LogInformation($"Round {currentRound} complete! All {currentRoundMatches.Count} matches finished.");
    
    // Check if next round already exists
    var nextRoundMatches = matches.Where(m => m.Round == currentRound + 1).ToList();
    if (nextRoundMatches.Any())
    {
        _logger.LogInformation($"Round {currentRound + 1} already exists. Skipping generation.");
        return;
    }
    
    // If only 1 match in current round, tournament is over
    if (currentRoundMatches.Count == 1)
    {
        var finalMatch = currentRoundMatches.First();
        await DeclareChampion(standingId, finalMatch.WinnerId.Value);
        return;
    }
    
    // Generate next round matches by pairing winners
    var winners = currentRoundMatches
        .OrderBy(m => m.Seed)
        .Select(m => m.WinnerId.Value)
        .ToList();
    
    int nextRound = currentRound + 1;
    int seed = 1;
    
    _logger.LogInformation($"Generating {winners.Count / 2} matches for Round {nextRound}");
    
    for (int i = 0; i < winners.Count; i += 2)
    {
        if (i + 1 < winners.Count)
        {
            var teamA = await _dbContext.Teams.FindAsync(winners[i]);
            var teamB = await _dbContext.Teams.FindAsync(winners[i + 1]);
            
            if (teamA != null && teamB != null)
            {
                await GenerateMatch(teamA, teamB, nextRound, seed++, standingId);
                _logger.LogInformation($"Generated match: {teamA.Name} vs {teamB.Name} (Round {nextRound}, Seed {seed - 1})");
            }
        }
    }
    
    await _dbContext.SaveChangesAsync();
    
    _logger.LogInformation($"Round {nextRound} successfully generated!");
}

private async Task DeclareChampion(long standingId, long championTeamId)
{
    var standing = await _standingrepository.Get(standingId);
    var tournament = await _tournamentRepository.Get(standing.TournamentId);
    
    // Mark standing as finished
    standing.IsFinished = true;
    standing.CanSetMatchScore = false;
    await _standingRepository.Update(standing);
    
    // Mark tournament as finished
    tournament.BracketFinished = true;
    tournament.TournamentStatus = TournamentStatus.Finished;
    tournament.IsFinished = true;
    await _tournamentRepository.Update(tournament);
    
    // Update champion status
    var championEntry = await _dbContext.BracketEntries
        .FirstOrDefaultAsync(b => b.TeamId == championTeamId && b.StandingId == standingId);
    
    if (championEntry != null)
    {
        championEntry.Status = TeamStatus.Champion;
    }
    
    await _dbContext.SaveChangesAsync();
    
    var team = await _dbContext.Teams.FindAsync(championTeamId);
    _logger.LogInformation($"🏆 TOURNAMENT COMPLETE! Champion: {team?.Name} (ID: {championTeamId})");
}
```

### Frontend Changes

**File:** `src/UI/src/app/components/standing/bracket/bracket.component.ts`

**Action:** Replace with updated implementation:

```typescript
export class BracketComponent implements OnInit {
  @Input() bracket: Standing | null = null;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();
  
  rounds: Match[][] = [];
  teams: Team[] = [];
  
  constructor(
    private matchService: MatchService,
    private teamService: TeamService
  ) {}

  ngOnInit(): void {
    this.loadBracket();
  }

  loadBracket(): void {
    if (!this.bracket) return;
    
    this.teamService.getAllTeams().subscribe(teams => {
      this.teams = teams;
      
      this.matchService.getMatchesByStandingId(this.bracket!.id).subscribe(matches => {
        this.organizeBracketRounds(matches);
      });
    });
  }

  organizeBracketRounds(matches: Match[]): void {
    if (!matches || matches.length === 0) {
      this.rounds = [];
      return;
    }
    
    const maxRound = Math.max(...matches.map(m => m.round));
    this.rounds = [];
    
    for (let round = 1; round <= maxRound; round++) {
      const roundMatches = matches
        .filter(m => m.round === round)
        .sort((a, b) => a.seed - b.seed);
      this.rounds.push(roundMatches);
    }
  }

  getRoundName(roundIndex: number): string {
    const matchesInRound = this.rounds[roundIndex].length;
    
    if (matchesInRound === 1) return 'Finals';
    if (matchesInRound === 2) return 'Semi-Finals';
    if (matchesInRound === 4) return 'Quarter-Finals';
    
    return `Round ${roundIndex + 1}`;
  }

  onMatchFinished(result: MatchFinishedIds): void {
    this.matchFinished.emit(result);
    
    setTimeout(() => {
      this.loadBracket();
    }, 500);
  }
}
```

**File:** `src/UI/src/app/components/standing/bracket/bracket.component.html`

**Action:** Replace template:

```html
<div class="bracket-container">
  <div class="bracket-round" *ngFor="let round of rounds; let roundIndex = index">
    <h3 class="round-title">{{ getRoundName(roundIndex) }}</h3>
    
    <div class="round-matches">
      <app-matches 
        [matches]="round"
        [teams]="teams"
        (matchFinished)="onMatchFinished($event)">
      </app-matches>
    </div>
  </div>
  
  <div *ngIf="rounds.length === 0" class="no-matches">
    <p>Bracket matches will appear here after groups are completed.</p>
  </div>
</div>
```

**File:** `src/UI/src/app/components/standing/bracket/bracket.component.css`

**Action:** Update styles:

```css
.bracket-container {
  display: flex;
  gap: 40px;
  padding: 20px;
  overflow-x: auto;
}

.bracket-round {
  min-width: 300px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.round-title {
  text-align: center;
  font-size: 18px;
  font-weight: 600;
  color: #333;
  padding: 10px;
  background: #f8f9fa;
  border-radius: 8px;
  margin: 0;
}

.round-matches {
  display: flex;
  flex-direction: column;
  gap: 40px;
  justify-content: space-around;
}

.no-matches {
  text-align: center;
  padding: 40px;
  color: #6c757d;
}
```

### Testing Checklist

**Test Case 1: 8-Team Bracket (3 Rounds)**
- [ ] Start from completed groups (8 teams in bracket)
- [ ] **Verify:** Round 1 shows 4 matches labeled "Quarter-Finals"
- [ ] Complete all 4 matches in Round 1
- [ ] **Verify:** Page auto-refreshes
- [ ] **Verify:** Round 2 appears with 2 matches labeled "Semi-Finals"
- [ ] **Verify:** Winners from Round 1 paired correctly
- [ ] Complete both Semi-Final matches
- [ ] **Verify:** Round 3 appears with 1 match labeled "Finals"
- [ ] Complete Finals match
- [ ] **Verify:** No Round 4 generated
- [ ] Check logs: Should show "TOURNAMENT COMPLETE"

**Test Case 2: 16-Team Bracket (4 Rounds)**
- [ ] Test with 16 teams advancing to bracket
- [ ] **Verify:** Round 1: 8 matches
- [ ] **Verify:** Round 2: 4 matches (Quarter-Finals)
- [ ] **Verify:** Round 3: 2 matches (Semi-Finals)
- [ ] **Verify:** Round 4: 1 match (Finals)

**Test Case 3: Bracket-Only Tournament**
- [ ] Create Format=BracketOnly, MaxTeams=8
- [ ] Add 8 teams, start tournament
- [ ] **Verify:** Round 1 immediately shows 4 matches
- [ ] Complete through to Finals
- [ ] **Verify:** Works same as bracket after groups

### Commit Message
```
feat: implement automatic bracket progression through rounds
```

---

## PHASE 5: Tournament Completion & Champion Display

**Goal:** Display champion prominently, show tournament as finished, prevent further changes.

### Backend Changes

**Note:** Most backend logic already implemented in Phase 4's `DeclareChampion` method. This phase focuses on frontend display.

### Frontend Changes

**File:** `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.html`

**Action:** Add champion banner at the top of the template:

```html
<!-- Champion Banner -->
<div *ngIf="tournament && tournament.isFinished" class="champion-banner">
  <div class="champion-content">
    <i class="pi pi-trophy champion-icon"></i>
    <div class="champion-info">
      <h2>Tournament Champion</h2>
      <h1>{{ getChampionName() }}</h1>
    </div>
    <i class="pi pi-trophy champion-icon"></i>
  </div>
</div>

<!-- Tournament Status Card -->
<p-card *ngIf="tournament" class="status-card">
  <div class="tournament-status">
    <h3>Status: 
      <span [ngClass]="getStatusClass()">{{ getTournamentStatus() }}</span>
    </h3>
    <div *ngIf="tournament.groupsFinished" class="status-item">
      ✓ Group Stage: Completed
    </div>
    <div *ngIf="tournament.bracketFinished" class="status-item">
      ✓ Bracket: Completed
    </div>
  </div>
</p-card>

<!-- Rest of existing template -->
```

**File:** `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.ts`

**Action:** Add helper methods:

```typescript
getChampionName(): string {
  if (!this.brackets || this.brackets.length === 0) {
    return 'Unknown';
  }
  
  const bracket = this.brackets[0];
  
  // Find BracketEntry with Champion status (status = 4)
  // Teams in bracket should have status property
  const championTeam = bracket.teams?.find((t: any) => t.status === 4);
  
  return championTeam ? championTeam.name : 'Tournament In Progress';
}

getTournamentStatus(): string {
  if (!this.tournament) return 'Unknown';
  
  switch (this.tournament.tournamentStatus) {
    case 0: return 'Upcoming';
    case 1: return 'In Progress';
    case 2: return 'Finished';
    default: return 'Unknown';
  }
}

getStatusClass(): string {
  if (!this.tournament) return '';
  
  switch (this.tournament.tournamentStatus) {
    case 0: return 'status-upcoming';
    case 1: return 'status-in-progress';
    case 2: return 'status-finished';
    default: return '';
  }
}
```

**File:** `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.css`

**Action:** Add champion banner styles:

```css
.champion-banner {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 40px;
  border-radius: 15px;
  margin-bottom: 30px;
  box-shadow: 0 10px 30px rgba(0,0,0,0.3);
}

.champion-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 30px;
  color: white;
}

.champion-icon {
  font-size: 64px;
  color: #ffd700;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.1); }
}

.champion-info {
  text-align: center;
}

.champion-info h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 400;
  opacity: 0.9;
}

.champion-info h1 {
  margin: 10px 0 0 0;
  font-size: 48px;
  font-weight: 700;
  text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
}

.status-card {
  margin-bottom: 20px;
}

.tournament-status {
  padding: 20px;
}

.tournament-status h3 {
  margin-top: 0;
}

.status-item {
  padding: 10px;
  margin: 10px 0;
  background: #d4edda;
  border-radius: 5px;
  color: #155724;
}

.status-upcoming {
  color: #0066cc;
  font-weight: bold;
}

.status-in-progress {
  color: #ff9900;
  font-weight: bold;
}

.status-finished {
  color: #28a745;
  font-weight: bold;
}
```

**File:** `src/UI/src/app/components/matches/matches.component.html`

**Action:** Disable buttons when tournament is finished:

```html
<button pButton
        type="button"
        icon="pi pi-plus"
        class="p-button-rounded p-button-primary"
        (click)="updateMatchScore(...)"
        [disabled]="match.winnerId || isUpdating[match.id] || isTournamentFinished">
</button>
```

**File:** `src/UI/src/app/components/matches/matches.component.ts`

**Action:** Add input property:

```typescript
@Input() isTournamentFinished: boolean = false;
```

**File:** Tournament detail/group/bracket components

**Action:** Pass the property down:

```html
<app-matches 
  [matches]="..."
  [teams]="..."
  [isTournamentFinished]="tournament?.isFinished"
  (matchFinished)="...">
</app-matches>
```

### Testing Checklist

**Full End-to-End Tournament Test:**
- [ ] Create tournament: 16 teams, 4 groups, 8-team bracket
- [ ] Add 16 teams
- [ ] Start tournament
- [ ] Complete all group matches
- [ ] **Verify:** Top teams advance, bracket seeds
- [ ] Complete Quarter-Finals
- [ ] **Verify:** Semi-Finals auto-generate
- [ ] Complete Semi-Finals
- [ ] **Verify:** Finals auto-generate
- [ ] Complete Finals
- [ ] **Verify:** Champion banner appears with winner's name
- [ ] **Verify:** Tournament status shows "Finished"
- [ ] **Verify:** All match buttons are disabled
- [ ] Refresh the page
- [ ] **Verify:** Champion banner persists
- [ ] Try clicking a match button
- [ ] **Verify:** Nothing happens (buttons disabled)
- [ ] Check database:
  - [ ] `Tournament.IsFinished = true`
  - [ ] `Tournament.TournamentStatus = 2`
  - [ ] `Tournament.BracketFinished = true`
  - [ ] Winner has `BracketEntry.Status = 4` (Champion)

**Edge Cases:**
- [ ] Navigate between tabs when tournament finished
- [ ] Test with Bracket-Only format
- [ ] Test with different bracket sizes (4, 8, 16, 32 teams)

### Commit Message
```
feat: display tournament champion and completion status
```

---

## Implementation Notes

### General Tips

1. **Commit Often:** After each phase passes testing, commit with the provided message
2. **Check Logs:** Backend logs provide valuable debugging info
3. **Database Verification:** Use a DB browser to verify status changes
4. **Test Incrementally:** Don't skip phases - each builds on the previous

### Common Issues & Solutions

**Issue:** Standings not updating after match
- **Check:** Is `UpdateStandingAfterMatch` being called in `SetGameResult`?
- **Check:** Is the method checking for `StandingType.Group`?

**Issue:** Groups not marking as finished
- **Check:** Are all matches in the group completed (all have `WinnerId`)?
- **Check:** Is `CheckAndMarkStandingAsFinishedAsync` being called?

**Issue:** Teams not advancing to bracket
- **Check:** Are all groups marked as `IsFinished = true`?
- **Check:** Is calculation correct: `TeamsPerBracket / NumberOfGroups`?
- **Check:** Verify `PrepareTeamsForBracket` is sorting correctly

**Issue:** Next round not generating
- **Check:** Are all current round matches finished?
- **Check:** Is `CheckAndGenerateNextRound` being called?
- **Check:** Check logs for generation messages

**Issue:** Champion not showing
- **Check:** Is `DeclareChampion` being called after finals?
- **Check:** Is `BracketEntry.Status = 4` for winner?
- **Check:** Is frontend looking for `status === 4`?

### Testing Strategy

1. **Unit Test Each Phase:** Complete testing checklist before moving on
2. **Use Same Tournament:** Keep one test tournament to run through all phases
3. **Document Edge Cases:** Note any unexpected behavior for future fixes
4. **Performance:** Test with maximum sizes (64 teams, 16 groups)

---

## Next Steps After Completion

Once all 5 phases are complete, the "Play Tournament" feature will be fully functional. The next major feature (Phase 6) would be "Finish Tournament" which includes:

- Prize distribution
- Tournament reports/statistics
- Export results
- Archive completed tournaments

But for now, focus on completing these 5 phases one at a time!

---

## Quick Reference

**Backend Key Files:**
- `GameService.cs` - Game results and match winners
- `StandingService.cs` - Group/bracket status management
- `MatchService.cs` - Match generation and progression

**Frontend Key Files:**
- `tournament-details.component.ts/html` - Main tournament view
- `group.component.ts/html` - Group standings display
- `bracket.component.ts/html` - Bracket visualization
- `matches.component.ts/html` - Match interaction

**Database Tables:**
- `GroupEntries` - Team status in groups (Wins/Losses/Points)
- `BracketEntries` - Team status in bracket
- `Matches` - Match data (Round, Seed, WinnerId)
- `Games` - Individual games within matches
- `Standings` - Group and Bracket standings (IsFinished flag)
- `Tournaments` - Overall status (GroupsFinished, BracketFinished, IsFinished)

Good luck with the implementation!