// Stand-in for team logos: initials on a colored shield, the same for the same name

// No green (stage colors) and no gold (winners only)
export const EMBLEM_COLORS = ['#3b6fd8', '#5b5bd6', '#8b5cf6', '#c2418f', '#d14343', '#1f8a99', '#4a6a8a', '#b5506b'];

// Words that say nothing about the team ("Team Nebula" = N)
const FILLER_WORDS = new Set(['team', 'esports', 'gaming', 'club']);

export interface TeamEmblem {
  initials: string;
  color: string;
}

export function teamEmblem(name: string): TeamEmblem {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const meaningful = words.filter((word) => !FILLER_WORDS.has(word.toLowerCase()));
  const used = meaningful.length ? meaningful : words;

  // Two words: both first letters; one word: its first letter, plus a digit right after it (G2, T1)
  let initials = used.length > 1 ? used[0][0] + used[1][0] : (used[0]?.match(/^.\d?/u)?.[0] ?? '?');
  initials = initials.toUpperCase();

  let hash = 0;
  for (const char of name.trim().toLowerCase()) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;

  return { initials, color: EMBLEM_COLORS[hash % EMBLEM_COLORS.length] };
}
