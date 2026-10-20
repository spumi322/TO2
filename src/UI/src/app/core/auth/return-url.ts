const DEFAULT_URL = '/tournaments';

// Only relative app paths: "//evil.com" or "https://..." would leave the app after login
export function safeReturnUrl(url: string | null | undefined): string {
  if (!url || !url.startsWith('/') || url.startsWith('//') || url.startsWith('/\\')) return DEFAULT_URL;
  if (url.startsWith('/login') || url.startsWith('/register')) return DEFAULT_URL;
  return url;
}
