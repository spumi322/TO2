import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'to2-theme';

// Sets data-theme on <html>; styles.css and the PrimeNG darkModeSelector read it. Default dark.
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(this.readStoredMode());

  init(): void {
    this.apply();
  }

  toggle(): void {
    this.mode.set(this.mode() === 'dark' ? 'light' : 'dark');
    try {
      localStorage.setItem(STORAGE_KEY, this.mode());
    } catch {
      // storage blocked: the choice lasts until reload
    }
    this.apply();
  }

  private apply(): void {
    document.documentElement.setAttribute('data-theme', this.mode());
  }

  private readStoredMode(): ThemeMode {
    try {
      return localStorage.getItem(STORAGE_KEY) === 'light' ? 'light' : 'dark';
    } catch {
      return 'dark';
    }
  }
}
