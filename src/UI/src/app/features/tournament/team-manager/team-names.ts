// Client checks for the bulk add input, for instant feedback only (the server checks again)

export const MAX_TEAM_NAME_LENGTH = 20;

export function parseTeamNames(input: string): string[] {
  return input
    .split(/[,\n]/)
    .map((name) => name.trim())
    .filter((name) => name.length > 0);
}

export function checkTeamNames(names: string[], registered: string[], freeSlots: number): string[] {
  const problems: string[] = [];
  const seen = new Set<string>();
  const registeredKeys = new Set(registered.map((name) => name.toLowerCase()));

  const tooLong = names.filter((name) => name.length > MAX_TEAM_NAME_LENGTH);
  if (tooLong.length) problems.push(`Longer than ${MAX_TEAM_NAME_LENGTH} characters: ${tooLong.join(', ')}`);

  const duplicates = names.filter((name) => {
    const key = name.toLowerCase();
    const duplicate = seen.has(key);
    seen.add(key);
    return duplicate;
  });
  if (duplicates.length) problems.push(`Listed twice: ${duplicates.join(', ')}`);

  const taken = names.filter((name) => registeredKeys.has(name.toLowerCase()));
  if (taken.length) problems.push(`Already registered: ${taken.join(', ')}`);

  if (names.length > freeSlots) {
    problems.push(freeSlots === 0 ? 'The tournament is full.' : `Only ${freeSlots} ${freeSlots === 1 ? 'slot' : 'slots'} left.`);
  }

  return problems;
}
