# UI stress test findings (2026-10-24)

Blind black-box browser test of the whole app (http://localhost:4200, user testuser-org1): every input field, every format, exotic and edge inputs, full life cycles, several tabs, keyboard, back/forward, light theme, 390px width (in a 390px iframe). Viewport 1920x863 with the browser's debug bar showing; overflow amounts are given in px. Nothing fixed yet.

Test tournaments left in Org1 (all prefixed `ST `):
- 337 (name with HTML/script, RTL, emoji, unicode; Groups+Bracket, finished)
- 338 (GroupsOnly, 3 groups underfilled; stuck in Groups live, finding 1)
- 339 (Bracket 5 of 32, finished; merged team names from finding 3)
- 340 (Groups+Bracket, 4 groups underfilled; cancelled with a long reason, finding 8)
- 342 (Registration, 1 team)
- 343 (32 teams, 8 groups, long names; finished)
- 344 (Bracket, 2 teams; finished)
- 345 (GroupsOnly, 5 teams in 2 groups; finished)

## Broken

### 1. Tournament stuck forever when a group gets one team (fixed 4d32e32)
- Repro A (GroupsOnly): max 9, 3 groups, register 3 teams, Start groups.
- Repro B (Groups+Bracket): max 12, 4 groups, advance 1, register 4 teams, Start groups.
- Result: start accepted, every group has 1 team and 0 matches (confirmed in the DB for 338 and 340). Status stays "Groups live, 0 of 0 matches played", the Groups segment stays 0/3, no Start bracket or finish ever appears; only Cancel gets out. Format card: "3 teams in 3 groups of 1, round robin".
- Expected: the server refuses the start, or every group has at least 2 teams.

### 2. Start offered although the server refuses (fixed 4d32e32)
- Repro: GroupsOnly, 3 groups, 2 teams registered.
- Result: UI shows Start groups ("At least 2 teams are needed to start"), dialog says "2 teams are drawn into 3 groups"; confirming gives the toast "Start groups failed: Not enough teams (2) for 3 groups". 2 teams are refused but 3 are accepted (leads to 1).
- Expected: start offered only when it can succeed.

### 3. Enter while a team add is saving merges names (fixed 3b9ea07; text typed in the moment before the field locks is still lost, not realistic)
- Where: Overview, Teams textarea (Enter adds).
- Repro: type "Y1", Enter, immediately type "Y2", Enter.
- Result: two POSTs at once, bodies `{"names":["Y1Y2"]}` (200) and `{"names":["Y1"]}` (409); a team "Y1Y2" is created and the toast says "Teams not added - Data has been modified by another user. Reload and try again." Typing Z1 Enter Z2 Enter ... Z5 fast created "Z1Z2Z3", three 409s, Z4/Z5 lost. Text typed while a save runs is dropped silently. Once 3 POSTs to `/api/tournaments/339/teams` returned 503 (not reproduced).
- Expected: one request at a time; the field locked or queued while saving.

### 4. Double click on Add team / Start groups shows a false error (fixed 3b9ea07)
- Repro: type "Echo", double click Add team; or double click Start groups in the confirm dialog.
- Result: the action succeeds, the second request gets 409 and a red toast ("Teams not added" / "Start groups failed - Data has been modified by another user").
- Expected: button disabled while the request runs (the score popup's Confirm already is).

## Wrong

### 6. Group ties decided by an order users can't see (fixed: head-to-head for 2 tied teams, then registration order)
- Repro: tournament 343 (8 groups of 4): in groups 2, 3, 4, 5, 7 and 8 the top two are both 2-1 with +2; the team that won the direct match is ranked 2nd (Group 7: T24 beat T04 2-1, T04 is 1st). 337 Group 2: 3-way tie (1-1, 0 each).
- The last tiebreaker is team id (documented rule), but the page only says "Ranked by match wins, then game difference". The tie decides who advances, winner vs runner-up, and bracket seeding.

### 7. Error text shows an internal state name; dialogs go stale (fixed: every user-facing server and validator message reviewed, statuses in words; console checked clean; stale dialogs not handled, the server's refusal is readable)
- Repro: open Start groups in two tabs, confirm in A, then in B.
- Result: toast "Groups cannot be started while the tournament is GroupsInProgress."
- Open confirm dialogs (Start groups / Start bracket) don't update or close when the state changes in another tab ("2 teams are seeded..." after a team was removed elsewhere).

### 8. Long unbroken word in the cancel reason overflows (fixed: all user-written text blocks wrap long words)
- Repro: cancel with a reason containing a 200-character word (tournament 340).
- Result: Overview status card text runs past the card, page scrolls sideways (scrollWidth 2031 vs 1920). At 390px even the headline "Cancelled by testuser-org1" is clipped. The list page truncates correctly.

### 9. Structure preview text wrong for some setups
- Create page, Groups+Bracket: with 1 group or advance 1 the preview still says "Group winners meet runners-up; teams of the same group are kept apart in round 1."
- 32 teams, 10 groups, advance 2: BYEs go to seeds 1-12, so round 1 is runners-up vs runners-up, contradicting that sentence.
- Format card: "The top 1 of each group advance..." (grammar).
- Bracket format is described as "random seeding" but the preview shows seed numbers (#1 vs #32).

## Cosmetic

### 10. Number fields handle out-of-range values inconsistently
- Max teams 40 is clamped to 32 on blur without a message, 3 shows an error; advance 20 is clamped to 16.
- Typing "-5.5e" becomes "55".
- Empty Max teams shows two errors, one of them the misleading "Each group needs at least 3 teams."
- Advance per group is capped at 16 even where more is valid (1 group of 32).

### 11. Team name length counts an emoji as 2 characters
- 11 trophy emojis: "Longer than 20 characters" with 11 visible characters; 10 are accepted.

### 12. Mixed wording and labels
- "Only 1 slot left." (team check) vs "1 place left" (panel).
- Top 4 card shows 6 rows for GroupsOnly with 3 groups (1st x3, 2nd x3; whole tiers by design).
- Popup header is "Semifinals" for both semifinals; groups show "Group 1 - Round 1".
- A BYE match popup shows "B5 0 / TBD 0, Confirmed" where the bracket shows "BYE / W".
- Tied ranks ordered Group 1 then 2 for 1st, but Group 2 then 1 for 2nd (Results podium and Top 4, tournament 345).
- Delete dialog: "its teams that are in no other tournament".

### 13. Drafts not shown on the bracket
- Another tab's draft score shows on the Groups tab match line; on the Bracket tab the box stays 0-0 (draft only inside the popup).

### 14. GroupsOnly results mark every non-winner red
- Finished GroupsOnly (345), Groups tab and Overview cards: 2nd place has the same red bar as last place, though nothing is eliminated in this format.

### 15. Direct URL to a tab the format doesn't have
- `/tournaments/339/groups` on a finished Bracket-only tournament: "Groups are not drawn yet - drawn when the tournament starts."

### 16. Phone width (390px)
- Results tab scrolls sideways (scrollWidth 404 vs 375).
- Groups tab W-L values and header wrap onto two lines ("2-" / "1").
- List table cut off: Stage/Format/Teams only after a sideways scroll inside the card.
- Long tournament names truncated with no way to read them on touch (title tooltip only).

### 17. Fit to one screen at 863px height
- 16-team bracket overflows by 108px (more than the ~40px debug bar).
- Create form scrolls inside its card and pushes Create tournament out of view once errors show.
- Overview with 8 groups has two nested scroll areas.
- List page with 12 In play rows overflows by 248px (partly by design: In play shows all rows).
- Overview group cards wrap a 20-character name onto 3 lines while the panel has free space.

### 18. Landing page and demo don't match
- Landing hero shows "Spring Championship - Groups live" with Alpha Squad etc.; /demo shows a finished "Spring Championship" with other teams.
- Demo Groups tab: Echo and Hotel each play 3 matches labelled R1 (old round numbering in the demo data).
- Landing page says the group table shows "points"; the real table has none.

### 19. Other
- No way to correct a confirmed result (popup on a confirmed match is read-only).
- Browser tab title is always "Tournament - TO2".
- Team emblem shows "T0"/"T1" for teams named T01...T32 (digit after the first letter rule).
- One console error: SignalR "WebSocket closed with status code 1006" during navigation.

## No problems found
- Tournament name: empty, whitespace only, padded under 4 characters (trimmed client side), 60 max.
- HTML/script, RTL, emoji and unicode names render escaped.
- Description: line breaks, 120 limit, whitespace only saves as empty (no About section).
- Team input: comma/newline split, blanks ignored, duplicates in the input and against registered teams (ignoring case), capacity check, 20 limit, live updates across tabs.
- Remove team: one click; double click sends one DELETE.
- Score popup: + capped at the wins needed, - disabled at 0, double click Confirm sends one request, simultaneous confirm from two tabs gives a clean "Match is already confirmed", drafts relay between tabs.
- Full life cycles: Groups+Bracket with 7 teams (4/3) and 32 teams (8 groups to 16, same-group pairs kept apart); Bracket 5 of 32 with 3 BYEs (semifinal scored before the quarterfinal); Bracket with 2 teams; GroupsOnly 5 in 2 groups. Result ranks (shared ranks, group positions) correct.
- Start after a team was removed in another tab: clean error.
- Cancel with reason; delete (the other tab is redirected with "Deleted by you (another tab)").
- 404 pages for deleted, non-numeric and unknown routes.
- Back/forward, triple Enter on the create form (one tournament created), light theme, list tabs and Show less/all, invite code dialog (opened only).

## Out of scope
- Keyboard-only use (accessibility): bracket match boxes can't be focused or opened; the score popup's focus isn't moved in or back; the format picker ignores arrow keys. Users of this project use a mouse; accessibility is a deliberate non-goal.

## Not tested
- Sign-in and register forms (signed in; signing out not allowed).
- Generating an invite code (changes an organization setting).
- Real window resizing (window stayed maximized; 390px tested in an iframe; small laptop height only as the 863px viewport).
