import { safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  it('keeps relative app paths', () => {
    expect(safeReturnUrl('/tournaments/5/groups')).toBe('/tournaments/5/groups');
  });

  it.each([null, '', 'https://evil.com', '//evil.com', '/\\evil.com', 'tournaments', '/login?returnUrl=/x'])(
    'falls back to the tournament list for %s',
    (url) => {
      expect(safeReturnUrl(url)).toBe('/tournaments');
    },
  );
});
