# brackets-viewer.js Implementation Guide
**Display-Only Bracket Visualization**

> **Goal**: Show full tournament bracket structure with first round populated, using brackets-viewer.js library for professional visualization.

---

## Overview

This implementation adds the brackets-viewer.js library to display tournament brackets. We're using the **Adapter Pattern** to transform TO2 backend data into the format the library expects.

**What this adds:**
- ✅ Professional bracket visualization (full structure)
- ✅ First round populated from `SeedBracket()` results
- ✅ Future rounds show as empty boxes (TBD)
- ✅ Clean separation: library handles display, backend handles logic

**What this does NOT do (yet):**
- ❌ Playing matches (coming in next phase)
- ❌ Round advancement logic
- ❌ Real-time updates

---

## Architecture

```
┌─────────────────┐
│  TO2 Backend    │ (Existing - NO CHANGES)
│  - Tournament   │
│  - Match        │
│  - Team         │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│  BracketAdapterService  │ (NEW)
│  Prepares data          │
│  structure for library  │
└────────┬────────────────┘
         │
         ▼
┌──────────────────────────┐
│  brackets-viewer.js      │ (LIBRARY)
│  Renders HTML/CSS for    │
│  bracket visualization   │
└──────────────────────────┘
```

---

## Important: What the Library Does

**The library (brackets-viewer.js):**
- ✅ Renders the bracket HTML/CSS (boxes, lines, layout)
- ✅ Handles visual layout and spacing
- ✅ Shows team names, scores, match states
- ❌ Does NOT generate tournament structure
- ❌ Does NOT know what rounds should exist

**We must provide:**
- Complete data structure (stages, rounds, matches)
- All match slots (even if opponents are TBD)
- The library renders based on what we give it

**Example:**
- If we give it only 4 matches → it renders only 4 boxes
- If we give it 7 matches (4+2+1) → it renders full 8-team bracket

This is why the adapter creates the full structure upfront.

---

## Step 1: Install Library via npm (5 minutes)

### 1.1 Install Package

```bash
cd src/UI
npm install brackets-viewer brackets-model
```

**Packages:**
- `brackets-viewer` - The visualization library
- `brackets-model` - TypeScript type definitions (optional but helpful)

### 1.2 Add to Angular Configuration

**File: `angular.json`**

Find the `"styles"` and `"scripts"` arrays under `"architect" > "build" > "options"` and add:

```json
{
  "projects": {
    "ui": {
      "architect": {
        "build": {
          "options": {
            "styles": [
              "src/styles.css",
              "node_modules/brackets-viewer/dist/brackets-viewer.min.css"
            ],
            "scripts": [
              "node_modules/brackets-viewer/dist/brackets-viewer.min.js"
            ]
          }
        }
      }
    }
  }
}
```

**Full context example:**
```json
"styles": [
  "src/styles.css",
  "node_modules/brackets-viewer/dist/brackets-viewer.min.css"
],
"scripts": [
  "node_modules/brackets-viewer/dist/brackets-viewer.min.js"
]
```

### 1.3 Restart Dev Server

After modifying `angular.json`, restart your dev server:

```bash
# Stop current server (Ctrl+C)
# Then restart
npm start
```

---

## Step 2: Create Adapter Service (20 minutes)

### File: `src/UI/src/app/services/bracket-adapter.service.ts` (NEW FILE)

**Create this new file** with the following content:

```typescript
import { Injectable } from '@angular/core';
import { Tournament } from '../models/tournament';
import { Match } from '../models/match';

@Injectable({
  providedIn: 'root'
})
export class BracketAdapterService {

  /**
   * Transform TO2 data to brackets-viewer format
   * 
   * IMPORTANT: We must provide the COMPLETE bracket structure (all rounds, all match slots)
   * even if opponents are TBD. The library renders based on the structure we provide.
   * 
   * If we only give Round 1 matches → library only renders Round 1
   * We want full bracket → we must provide all rounds with empty slots
   * 
   * @param tournament - TO2 Tournament with teams
   * @param matches - TO2 Match array (with Round and Seed)
   * @returns Complete data structure for brackets-viewer.js or null
   */
  transformToBracketsViewer(tournament: Tournament, matches: Match[]) {
    // Filter only bracket matches (those with Round set)
    const bracketMatches = matches.filter(m => m.round != null);
    
    if (bracketMatches.length === 0) {
      console.warn('No bracket matches found');
      return null;
    }

    // Calculate total rounds needed
    const maxRound = Math.max(...bracketMatches.map(m => m.round!));
    const totalRounds = maxRound;

    // 1. Create synthetic stage (library requires this)
    const stage = [{
      id: 1,
      tournament_id: tournament.id,
      name: 'Main Bracket',
      type: 'single_elimination',
      number: 1
    }];

    // 2. Create synthetic group (library requires this)
    const group = [{
      id: 1,
      stage_id: 1,
      number: 1,
      name: 'Bracket'
    }];

    // 3. Define all rounds upfront
    // Library needs to know: "There are 3 rounds total"
    const rounds = [];
    for (let i = 1; i <= totalRounds; i++) {
      rounds.push({
        id: i,
        stage_id: 1,
        group_id: 1,
        number: i,
        name: this.getRoundName(i, totalRounds)
      });
    }

    // 4. Create match data structure for library
    // We provide ALL match slots (library decides how to render them)
    const viewerMatches = [];
    let matchIdCounter = 1;

    for (let roundNum = 1; roundNum <= totalRounds; roundNum++) {
      const round = rounds.find(r => r.number === roundNum)!;
      const matchesInThisRound = Math.pow(2, totalRounds - roundNum);

      for (let i = 0; i < matchesInThisRound; i++) {
        // Look up actual match data (only exists for Round 1 initially)
        const actualMatch = bracketMatches.find(
          m => m.round === roundNum && m.seed === i + 1
        );

        // Tell library about this match slot
        viewerMatches.push({
          id: matchIdCounter++,
          stage_id: 1,
          group_id: 1,
          round_id: round.id,
          number: i + 1,
          child_count: 0,
          status: actualMatch ? 'ready' : 'locked', // locked = not ready yet (TBD)
          opponent1: actualMatch?.teamAId ? {
            id: actualMatch.teamAId,
            score: 0
          } : null, // null = TBD slot
          opponent2: actualMatch?.teamBId ? {
            id: actualMatch.teamBId,
            score: 0
          } : null
        });
      }
    }

    // 5. Transform participants (teams)
    const participants = tournament.teams?.map(team => ({
      id: team.id,
      tournament_id: tournament.id,
      name: team.name
    })) || [];

    console.log('Bracket structure prepared for library:', {
      stages: stage.length,
      rounds: rounds.length,
      matches: viewerMatches.length,
      participants: participants.length
    });

    // Return complete structure - library will render it
    return {
      stage: stage,
      group: group,
      round: rounds,
      match: viewerMatches,
      match_game: [],
      participant: participants
    };
  }

  /**
   * Generate round names (Final, Semi-finals, etc.)
   */
  private getRoundName(roundNumber: number, totalRounds: number): string {
    const fromEnd = totalRounds - roundNumber + 1;
    
    switch(fromEnd) {
      case 1: return 'Final';
      case 2: return 'Semi-finals';
      case 3: return 'Quarter-finals';
      case 4: return 'Round of 16';
      case 5: return 'Round of 32';
      default: return `Round ${roundNumber}`;
    }
  }
}
```

**Key Points:**
- We prepare the **data structure** (not HTML)
- Library receives this structure and **renders the HTML**
- We must include all match slots so library knows full bracket shape
- `null` opponents = TBD boxes in the rendered bracket

---

## Step 3: Update Bracket Component (15 minutes)

### File: `src/UI/src/app/components/standing/bracket/bracket.component.ts`

**Replace the entire file** with:

```typescript
import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { Tournament } from '../../../models/tournament';
import { MatchService } from '../../../services/match/match.service';
import { BracketAdapterService } from '../../../services/bracket-adapter.service';
import { Match } from '../../../models/match';

@Component({
  selector: 'app-standing-bracket',
  templateUrl: './bracket.component.html',
  styleUrls: ['./bracket.component.css']
})
export class BracketComponent implements OnInit, OnChanges {
  @Input() tournament!: Tournament;
  @Input() standingId!: number;

  matches: Match[] = [];
  isLoading = true;

  constructor(
    private matchService: MatchService,
    private bracketAdapter: BracketAdapterService
  ) {}

  ngOnInit() {
    this.loadAndRenderBracket();
  }

  ngOnChanges() {
    this.loadAndRenderBracket();
  }

  /**
   * Load matches from backend and render bracket
   */
  loadAndRenderBracket() {
    if (!this.standingId) {
      console.warn('No standingId provided');
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    
    this.matchService.getMatchesByStandingId(this.standingId).subscribe({
      next: (matches) => {
        console.log('Matches loaded:', matches.length);
        this.matches = matches;
        this.isLoading = false;
        
        // Render bracket after short delay (let DOM update)
        setTimeout(() => this.renderBracket(), 100);
      },
      error: (err) => {
        console.error('Error loading matches:', err);
        this.isLoading = false;
      }
    });
  }

  /**
   * Render bracket using brackets-viewer.js library
   * The library does ALL the rendering (HTML/CSS)
   * We just provide the data structure
   */
  renderBracket() {
    // Check if library is loaded
    if (!(window as any).bracketsViewer) {
      console.error('brackets-viewer library not loaded! Check angular.json configuration');
      return;
    }

    // Transform data using adapter
    const data = this.bracketAdapter.transformToBracketsViewer(
      this.tournament, 
      this.matches
    );
    
    if (!data) {
      console.warn('No bracket data to display');
      return;
    }

    console.log('Calling bracketsViewer.render() - library will handle all rendering');

    // Call library to render - it creates all HTML/CSS
    (window as any).bracketsViewer.render(data, {
      selector: '.brackets-viewer',
      clear: true
    });
  }
}
```

**What happens:**
1. Component loads match data from backend
2. Adapter transforms to library format
3. `bracketsViewer.render()` creates all HTML/CSS
4. No manual HTML generation by us!

---

### File: `src/UI/src/app/components/standing/bracket/bracket.component.html`

**Replace the entire file** with:

```html
<div class="bracket-container">
  <!-- Loading state -->
  <div class="loading-state" *ngIf="isLoading">
    <mat-spinner diameter="50"></mat-spinner>
    <p>Loading bracket...</p>
  </div>

  <!-- Bracket visualization (library injects HTML here) -->
  <div class="brackets-viewer" *ngIf="!isLoading && matches.length > 0"></div>

  <!-- Empty state -->
  <div class="empty-state" *ngIf="!isLoading && matches.length === 0">
    <mat-icon class="empty-icon">emoji_events</mat-icon>
    <p>No bracket matches yet</p>
    <p class="hint">Start the tournament to generate bracket</p>
  </div>
</div>
```

**Key element:**
- `.brackets-viewer` - Empty div where library injects its HTML
- We don't create match boxes - library does!

---

### File: `src/UI/src/app/components/standing/bracket/bracket.component.css`

**Replace the entire file** with:

```css
.bracket-container {
  padding: 20px;
  min-height: 400px;
}

/* Loading state */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px;
  gap: 16px;
}

.loading-state p {
  color: #666;
  margin: 0;
  font-size: 14px;
}

/* Bracket viewer container */
.brackets-viewer {
  background: #1a1a1a;
  padding: 20px;
  border-radius: 8px;
  overflow-x: auto;
  min-height: 400px;
}

/* Empty state */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px;
  color: #666;
  text-align: center;
}

.empty-icon {
  font-size: 64px;
  width: 64px;
  height: 64px;
  color: #bdbdbd;
  margin-bottom: 16px;
}

.empty-state p {
  margin: 8px 0;
  font-size: 16px;
}

.empty-state .hint {
  font-size: 14px;
  color: #999;
}

/* Custom theme overrides for brackets-viewer */
:root {
  --primary-color: #1976d2;
  --text-color: #e0e0e0;
  --background-color: #1a1a1a;
}
```

---

## Step 4: Update Parent Component (5 minutes)

### File: `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.html`

**Find the Bracket tab** and update it to pass `standingId`:

```html
<!-- Bracket Tab (only if tournament has started) -->
<mat-tab label="Bracket" *ngIf="!tournament?.isRegistrationOpen">
  <div class="tab-content">
    <app-standing-bracket 
      [tournament]="tournament"
      [standingId]="brackets[0]?.id">
    </app-standing-bracket>
  </div>
</mat-tab>
```

**What changed:**
- Added `[standingId]="brackets[0]?.id"` input binding
- Component needs this to fetch matches

---

## Step 5: Test Implementation (10 minutes)

### Testing Steps

1. **Start Angular dev server:**
   ```bash
   cd src/UI
   npm start
   ```

2. **Open browser:** Navigate to `https://localhost:4200` (or your configured port)

3. **Create/Open a tournament:**
   - Tournament must have status = `Ongoing` (not `Upcoming`)
   - Tournament must have bracket matches generated

4. **Click "Bracket" tab**

5. **Verify you see:**
   - ✅ Full bracket structure displayed
   - ✅ Round 1 has actual team names
   - ✅ Round 2, 3, etc. show empty boxes (TBD)
   - ✅ Proper round labels (Quarter-finals, Semi-finals, Final)
   - ✅ Professional layout with connecting lines

### Expected Visual Result

```
Quarter-finals        Semi-finals           Final
┌─────────────┐
│   Team A    │───┐     
└─────────────┘   │    ┌───────┐
                  ├────│       │
┌─────────────┐   │    │  TBD  │
│   Team B    │───┘    │       │───┐
└─────────────┘        └───────┘   │
                                   │  ┌────────┐
┌─────────────┐                    ├──│  TBD   │
│   Team C    │───┐    ┌───────┐   │  └────────┘
└─────────────┘   │    │       │───┘
                  ├────│  TBD  │
┌─────────────┐   │    │       │
│   Team D    │───┘    └───────┘ 
└─────────────┘
```

---

## Troubleshooting

### Problem: "bracketsViewer is not defined"

**Symptom:** Browser console error: `bracketsViewer is not defined`

**Cause:** npm package not loaded in `angular.json`

**Solutions:**
1. Verify `angular.json` has library in `"scripts"` array
2. Restart dev server after modifying `angular.json`
3. Check browser DevTools > Sources > webpack:// > node_modules > brackets-viewer
4. Try hard refresh: `Ctrl+F5` (Windows) or `Cmd+Shift+R` (Mac)

---

### Problem: Styles not applied

**Symptom:** Bracket elements visible but unstyled

**Cause:** CSS not loaded from `angular.json`

**Solutions:**
1. Verify `angular.json` has library in `"styles"` array
2. Restart dev server
3. Check browser DevTools > Sources for `brackets-viewer.min.css`
4. Clear browser cache

---

### Problem: Empty white box instead of bracket

**Symptom:** Component renders but bracket area is blank

**Solutions:**
1. Open browser console - check for errors
2. Verify `matches.length > 0` (check Network tab API call)
3. Check if `Match.Round` field is populated in database
4. Add `console.log()` in `transformToBracketsViewer()` to debug data

---

### Problem: "No bracket matches found"

**Symptom:** Console warning: "No bracket matches found"

**Cause:** Matches don't have `Round` field set

**Solution:**
1. Verify backend sets `Match.Round` when generating matches
2. Check database: `SELECT * FROM Matches WHERE Round IS NOT NULL`
3. Ensure `SeedBracket()` was called before rendering

---

### Problem: Bracket doesn't update after data changes

**Symptom:** Old bracket data shown after tournament progresses

**Solution:**
1. Add `ngOnChanges()` detection (already included in code)
2. Force reload: Call `loadAndRenderBracket()` after data changes
3. Check if `@Input() tournament` reference changes (not just properties)

---

### Problem: Component can't find BracketAdapterService

**Symptom:** TypeScript error: `Cannot find name 'BracketAdapterService'`

**Solution:**
1. Verify file created at: `src/UI/src/app/services/bracket-adapter.service.ts`
2. Check `@Injectable({ providedIn: 'root' })` decorator present
3. Restart `ng serve` to pick up new service
4. Import statement: `import { BracketAdapterService } from '../../../services/bracket-adapter.service';`

---

### Problem: npm install fails

**Symptom:** Error during `npm install brackets-viewer`

**Solutions:**
1. Try clearing npm cache: `npm cache clean --force`
2. Delete `node_modules` and `package-lock.json`, then `npm install`
3. Check Node.js version (needs 14+)
4. Try with `--legacy-peer-deps`: `npm install brackets-viewer --legacy-peer-deps`

---

## Browser Console Debugging

Add these checks in browser console:

```javascript
// Check if library loaded
window.bracketsViewer
// Should show: {render: ƒ, ...}

// Check component data
angular.getComponent(document.querySelector('app-standing-bracket'))
// Should show: {tournament: {...}, matches: [...], ...}

// Manual render test
window.bracketsViewer.render({
  stage: [{id: 1, tournament_id: 1, name: 'Test', type: 'single_elimination', number: 1}],
  group: [{id: 1, stage_id: 1, number: 1, name: 'Test'}],
  round: [{id: 1, stage_id: 1, group_id: 1, number: 1, name: 'Round 1'}],
  match: [],
  match_game: [],
  participant: []
}, {selector: '.brackets-viewer', clear: true});
// Should render empty bracket structure
```

---

## Verification Checklist

Before considering this done:

- [ ] `npm install brackets-viewer brackets-model` completed
- [ ] `angular.json` updated with styles and scripts
- [ ] Dev server restarted after `angular.json` changes
- [ ] `bracket-adapter.service.ts` created with correct path
- [ ] `bracket.component.ts` updated (uses adapter + library)
- [ ] `bracket.component.html` updated (`.brackets-viewer` div present)
- [ ] `bracket.component.css` updated (dark theme styling)
- [ ] Parent component passes `[standingId]` input
- [ ] No TypeScript compilation errors
- [ ] No browser console errors
- [ ] Bracket displays with Round 1 populated
- [ ] Future rounds show empty boxes (TBD)
- [ ] Round names display correctly

---

## What's Next? (Future Phases)

This implementation is **display-only**. Next steps will add:

### Phase 2: Play Matches
- Click match to open dialog
- Reuse existing `MatchesComponent` for scoring
- Update bracket when match completes

### Phase 3: Round Advancement
- Detect when current round finishes
- Generate next round matches automatically
- Advance winners to next round

### Phase 4: Tournament Completion
- Detect when tournament finishes
- Display champion
- Show final standings

---

## Code Summary

### Files Modified
1. `angular.json` (2 arrays updated: styles + scripts)
2. `src/UI/src/app/components/standing/bracket/bracket.component.ts` (replaced)
3. `src/UI/src/app/components/standing/bracket/bracket.component.html` (replaced)
4. `src/UI/src/app/components/standing/bracket/bracket.component.css` (replaced)
5. `src/UI/src/app/components/tournament/tournament-details/tournament-details.component.html` (1 attribute added)

### Files Created
1. `src/UI/src/app/services/bracket-adapter.service.ts` (new)

### Packages Added
1. `brackets-viewer` (npm)
2. `brackets-model` (npm)

### Files NOT Modified
- Backend (zero changes)
- Models
- Other components
- Other services

---

## Time Estimate

| Task | Time |
|------|------|
| Step 1: npm install | 5 min |
| Step 2: Create adapter | 15 min |
| Step 3: Update component | 15 min |
| Step 4: Update parent | 3 min |
| Step 5: Test & debug | 10 min |
| **Total** | **~50 minutes** |

---

## Success Criteria

✅ You've successfully implemented brackets-viewer.js when:

1. Opening a started tournament shows the Bracket tab
2. Clicking Bracket tab displays full bracket structure
3. Round 1 shows actual team names from seeding
4. Future rounds show "TBD" placeholders
5. No console errors in browser
6. Bracket is responsive and scrollable
7. Round names display correctly (Semi-finals, Final, etc.)
8. Library is rendering the HTML (not our custom code)

---

## Understanding: We Provide Structure, Library Renders

**Common Confusion:**
"Why are we creating empty match objects? Shouldn't the library do that?"

**Answer:**
The library is a **rendering engine**, not a tournament generator.

**Analogy:**
```
Library = Printer
Our Adapter = Document Creator

We create the document (data structure)
Library prints it beautifully (renders HTML/CSS)
```

**What we provide:**
```typescript
{
  rounds: 3,
  matches: [
    {round: 1, opponent1: "Team A", opponent2: "Team B"},
    {round: 1, opponent1: "Team C", opponent2: "Team D"},
    {round: 2, opponent1: null, opponent2: null}, // ← TBD slot
    {round: 3, opponent1: null, opponent2: null}  // ← TBD slot
  ]
}
```

**What library renders:**
```
Beautiful HTML/CSS bracket with:
- Round 1 showing Team A vs Team B
- Round 2 showing empty "TBD" boxes
- Round 3 showing empty "TBD" boxes
- Connecting lines between rounds
```

If we only provided Round 1 matches, library would only render Round 1!

---

## Additional Resources

- **Library Docs:** https://drarig29.github.io/brackets-docs/
- **GitHub:** https://github.com/Drarig29/brackets-viewer.js
- **npm Package:** https://www.npmjs.com/package/brackets-viewer
- **Demo:** https://github.com/Drarig29/brackets-viewer.js/tree/master/demo

---

## Support

If you encounter issues:
1. Check browser console for errors
2. Verify all files were updated correctly
3. Review Troubleshooting section above
4. Compare your code with this guide

**Good luck with implementation!** 🚀
