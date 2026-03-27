

# Groups and Bracket
GroupSBracketMaxteams-Numberofgroups-Advancingpergroup

// Test case #1 minimum participants
GSB4-1-1 -- groups OK, bracket failed, stopped at seeding(probably 1 advancing from 1 group is not a valid bracket)
GSB4-1-3 -- groups OK, bracket OK, tournament finished

// Test case #2 maximum participants
GSB32-1-1 -- UI is 1 big scrollable wall, groups OK, bracket failed, same as before
GSB32-10-2 -- groups OK, UI a bit weird with uneven groups, bracket OK with many BYEs, tournament finished
GSB32-8-1 -- groups OK, bracket OK, tournament finished

// Test case #3 avg participants
GSB16-2-15 -- groups OK, bracket with BYEs OK, tournament finished
GSB16-5-2 -- groups OK, UI weird with uneven groups, bracket with BYEs OK, tournament finished

# Groups Only
GroupSMaxteams-NUmberofgroups

// Test case #1 minimum participants
GS4-1 -- OK, tournament finished

// Test case #2 maximum participants
GS32-10 -- UI shows top1 team of each group as champion, with 10 groups thats 10 champ? looks weird, final results looks weird too, otherwise OK.

// Test case #3 avg participants
GS16-4 --  exact same as test case #2

# Bracket Only
BracketMaxteams

// Test case #1 minimum participants
BRCKT4 -- OK
BRCKT5 -- OK

// Test case #2 maximum participants
BRCKT-31 -- OK, BYEs handled

// Test case #3 avg participants
BRCKT-16 -- OK

