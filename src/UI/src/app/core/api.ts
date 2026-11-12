import { environment } from '../../environments/environment';

export const API_URL = environment.apiUrl;

export function apiUrl(path: string): string {
  return `${API_URL}/${path}`;
}

// The bearer token goes to our API only, never to other origins
export function isApiRequest(url: string): boolean {
  return url.startsWith(`${API_URL}/`);
}
