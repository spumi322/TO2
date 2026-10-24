// Sentences about a format's rules, shared by the create preview, the Overview and the start dialogs

// "The top 2 of each group advance" / "The winner of each group advances"
export function advanceText(advancing: number): string {
  return advancing === 1 ? 'The winner of each group advances' : `The top ${advancing} of each group advance`;
}

// How group results seed the bracket (as the server's group result seeding does); null when it says nothing new:
// with one group there is no other group, with one advancing team per group all entrants are group winners
export function groupSeedingText(groups: number, advancing: number): string | null {
  return groups > 1 && advancing > 1
    ? 'Group winners are seeded first and BYEs go to the top seeds; teams of the same group are kept apart in round 1 where possible.'
    : null;
}
