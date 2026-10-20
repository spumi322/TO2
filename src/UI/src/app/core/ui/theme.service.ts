import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'to2-theme';

// Sets data-theme on <html>; styles.css and the PrimeNG darkModeSelector read it
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly media = window.matchMedia('(prefers-color-scheme: dark)');
  readonly mode = signal<ThemeMode>(this.readStoredMode());

  init(): void {
    this.media.addEventListener('change', () => this.apply());
    this.apply();
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // storage blocked: the choice lasts until reload
    }
    this.apply();
  }

  private apply(): void {
    const mode = this.mode();
    const dark = mode === 'dark' || (mode === 'system' && this.media.matches);
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
  }

  private readStoredMode(): ThemeMode {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'light' || stored === 'dark' ? stored : 'system';
    } catch {
      return 'system';
    }
  }
}
