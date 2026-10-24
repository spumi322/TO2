import { EMBLEM_COLORS, teamEmblem } from './team-emblem';

describe('teamEmblem', () => {
  it('takes initials from the meaningful words', () => {
    expect(teamEmblem('Team Nebula').initials).toBe('N');
    expect(teamEmblem('Vortex Gaming').initials).toBe('V');
    expect(teamEmblem('Iron Hawks').initials).toBe('IH');
    expect(teamEmblem('night owls esports').initials).toBe('NO');
  });

  it('keeps a digit after the first letter of a single word', () => {
    expect(teamEmblem('G2 Esports').initials).toBe('G2');
    expect(teamEmblem('Phoenix').initials).toBe('P');
  });

  it('uses all words when every word is filler', () => {
    expect(teamEmblem('Team Gaming').initials).toBe('TG');
  });

  it('gives the same name the same color, ignoring case', () => {
    const color = teamEmblem('Iron Hawks').color;
    expect(EMBLEM_COLORS).toContain(color);
    expect(teamEmblem('iron hawks').color).toBe(color);
  });
});
