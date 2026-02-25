# Devnote: Tournament Configurator

> **Note:** Two features discussed in this devnote are not yet implemented and require separate planning:
> - **BYE system** — bracket padding, auto-advancement of BYE matches, pipeline changes
> - **Unequal Group Advancement Logic** — win-rate based cross-group ranking, `CrossGroupRankingService`

---

## What it is

A single-page interactive tournament configurator that replaces the existing multi-field form. Instead of filling out fields and submitting blind, the user configures a tournament while seeing a live structural preview that updates in real time. The preview is the confirmation — there is no separate review step.

---

## What it does

The user provides four things at most: a name, a description, a format, and a handful of numbers. Everything else — bracket size, BYE count, group distribution, advancement slots — is computed and displayed immediately as they type. Invalid combinations cannot be submitted because invalid options are never offered. The user sees exactly what the system will build before they commit.

---

## How it works

The layout is two columns. Left side holds the input fields. Right side holds the live preview panel. As the user changes any input, the right side re-renders instantly showing the group cards, bracket structure, seeding slots, BYE indicators, and a warning chip when groups are unequal. The submit button at the bottom is disabled until name is filled and the configuration is structurally valid. There is no step navigation, no back button, no confirmation modal.

The inputs vary by format:

- **BracketOnly** — name, description, max teams (3–32 free input). Bracket size and BYE count are computed.
- **GroupsOnly** — name, description, max teams, number of groups (options filtered so minimum group size is 3). Group distribution is computed.
- **GroupsAndBracket** — name, description, max teams, number of groups, teams advancing per group (options filtered to leave at least 1 eliminated per group). Bracket size, BYE count, and group distribution are all computed.

---

## How it integrates

This touches more than just the form. The full change surface is documented below.

### DTO changes

`CreateTournamentRequestDTO` requires two changes:

- Add `NumberOfGroups` (replaces `TeamsPerGroup` as the user-facing input for GroupsOnly and GroupsAndBracket)
- Add `AdvancingPerGroup` (new field, required for GroupsAndBracket only)
- `TeamsPerGroup` becomes a computed value derived server-side from `MaxTeams / NumberOfGroups` — it is no longer a user input and should not be accepted from the request

### New field on Tournament entity

`AdvancingPerGroup` must be persisted on the `Tournament` entity or the bracket `Standing`. It is a user decision made at creation time and must be available at bracket seeding time. Currently `GetTeamsForBracketByFormat` derives advancement count as `bracket.MaxTeams / groups.Count` — this breaks with unequal groups and does not reflect the organizer's intent. The stored value becomes the source of truth.

### Standing initialization rewrite

`InitializeStandingsForTournamentAsync` currently creates every group standing with the same uniform `teamsPerGroup` value. With unequal groups (e.g. 11 teams / 3 groups → 4/4/3), each standing must be initialized with its actual computed size. The method needs to run `distributeTeams(maxTeams, numberOfGroups)` and create each standing individually with the correct size rather than a single loop with a fixed value.

### Validator changes

`CreateTournamentValidator` requires the following rule removals:

- Remove `IsPowerOfTwo` check on `MaxTeams` for BracketOnly
- Remove `IsPowerOfTwo` check on `TeamsPerBracket` for GroupsAndBracket
- Remove `MaxTeams == TeamsPerBracket` equality check for BracketOnly
- Replace `TeamsPerGroup`-based divisibility rules with `NumberOfGroups`-based minimum group size check (`MaxTeams / NumberOfGroups >= 3`)

All other validation rules stay. The frontend `powerOfTwoValidator` and hardcoded `bracketSizeOptions` array (`[4, 8, 16, 32]`) are removed entirely. The `divisibleByValidator` is removed from the frontend — the backend rejects structurally invalid configurations as a final safeguard only.

### New Angular service: `TournamentConfigService`

Encapsulates all computation logic shared between the preview and the submit payload:

- `nextPowerOfTwo(n)`
- `distributeTeams(maxTeams, numberOfGroups)` → `number[]`
- `computeBracketSize(format, maxTeams, numberOfGroups, advancingPerGroup)` → `number`
- `computeByeCount(bracketSize, teamCount)` → `number`
- `buildCreateRequest(formState)` → `CreateTournamentRequestDTO`

The configurator component consumes this service for live preview. On submit, `buildCreateRequest` is called once to produce the final DTO. The same math the user sees is what gets sent.

### BYE pipeline (not yet implemented)

The following backend changes are required but deferred:

- `CalculateBracketStructureStep` — pad `AdvancedTeams` to next power of 2 with `null` entries, adjust `TotalRounds`
- `BracketSeedingUtility` — add `PadToPowerOfTwo`, update pair type to `(Team?, Team?)`
- `GenerateBracketMatchesStep` — detect null-team pairs, generate match, immediately auto-resolve with the real team as winner
- `StartBracketContext.SeededPairs` — change type from `List<(Team, Team)>` to `List<(Team?, Team?)>`

### Unequal group ranking (not yet implemented)

The following backend changes are required but deferred:

- New `CrossGroupRankingService` implementing win-rate ranking with tiebreaker chain: win rate → head-to-head within group → total wins → total losses ascending
- `GetTeamsForBracketByFormat` — replace raw-points advancement with `CrossGroupRankingService`
- `GetFinalResultsFromTournamentTeams` — replace raw-wins final standings with `CrossGroupRankingService` for GroupsOnly

### Test updates

`CreateTournamentValidatorTests` contains explicit assertions that non-power-of-2 values fail validation. These tests must be updated to assert the new rules — not deleted. Coverage on the new minimum group size constraint, `NumberOfGroups` validation, and `AdvancingPerGroup` bounds must be added.

---

## Preview Component Reference

The following React prototype demonstrates the intended configurator UX. It is not production code — it serves as a visual and behavioral specification for the Angular implementation.

```jsx
import { useState, useMemo } from "react";

const PRIMARY = "#00d936";
const BG_DARK = "#0a0e1a";
const BG_CARD = "#111827";
const BG_CARD2 = "#1a2235";
const BORDER = "#1e2d40";
const TEXT = "#e2e8f0";
const TEXT_MUTED = "#64748b";
const BYE_COLOR = "#1e3a2a";
const BYE_BORDER = "#2d5a3d";

function nextPow2(n) {
  if (n <= 1) return 2;
  let p = 1;
  while (p < n) p <<= 1;
  return p;
}

function distributeTeams(total, groups) {
  const base = Math.floor(total / groups);
  const extra = total % groups;
  return Array.from({ length: groups }, (_, i) => base + (i < extra ? 1 : 0));
}

function generateSeedingOrder(n) {
  const order = new Array(n);
  order[0] = 0;
  order[1] = n - 1;
  let filled = 2;
  const rounds = Math.log2(n);
  for (let r = 1; r < rounds; r++) {
    const step = n / Math.pow(2, r + 1);
    const cur = filled;
    for (let i = 0; i < cur; i += 2) {
      order[filled++] = order[i] + step;
      order[filled++] = order[i + 1] - step;
    }
  }
  return order;
}

// ... (full component implementation in artifact)
```

Key behaviors to replicate in Angular:

- Format selection immediately resets and re-validates all dependent fields
- `NumberOfGroups` options are filtered dynamically: only values where `maxTeams / numberOfGroups >= 3` are offered
- `AdvancingPerGroup` options are filtered dynamically: maximum is `minGroupSize - 1`
- Bracket size, BYE count, and group distribution update on every input change with no debounce
- Unequal group warning chip appears automatically when `new Set(groupSizes).size > 1`
- Submit is disabled until name is non-empty and all required fields for the selected format are valid
