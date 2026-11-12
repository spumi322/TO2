export const REFRESH_TOKEN_KEY = 'to2-refresh-token';

// localStorage can throw (private mode, blocked storage): then the session just doesn't survive a reload
export const refreshTokenStorage = {
  read(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      return null;
    }
  },
  write(token: string): void {
    try {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } catch {
      // ignore
    }
  },
  remove(): void {
    try {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
    } catch {
      // ignore
    }
  },
};
