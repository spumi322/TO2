import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Button } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { AuthService } from '../core/auth/auth.service';
import { SessionStore } from '../core/auth/session.store';
import { RealtimeService } from '../core/realtime/realtime.service';
import { ThemeMode, ThemeService } from '../core/ui/theme.service';
import { InviteCodeDialog } from './invite-code-dialog';

@Component({
  selector: 'app-top-bar',
  imports: [RouterLink, RouterLinkActive, Button, Menu, InviteCodeDialog],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.css',
})
export class TopBar {
  private readonly auth = inject(AuthService);
  readonly session = inject(SessionStore);
  readonly realtime = inject(RealtimeService);

  readonly theme = inject(ThemeService);
  readonly inviteOpen = signal(false);

  readonly themeIcon = computed(() => {
    switch (this.theme.mode()) {
      case 'light':
        return 'pi pi-sun';
      case 'dark':
        return 'pi pi-moon';
      default:
        return 'pi pi-desktop';
    }
  });

  readonly themeMenu = computed<MenuItem[]>(() =>
    (['system', 'light', 'dark'] as ThemeMode[]).map((mode) => ({
      label: mode === 'system' ? 'System' : mode === 'light' ? 'Light' : 'Dark',
      icon: this.theme.mode() === mode ? 'pi pi-check' : 'pi pi-fw',
      command: () => this.theme.setMode(mode),
    })),
  );

  readonly liveLabel = computed(() => {
    switch (this.realtime.state()) {
      case 'connected':
        return 'Live updates on';
      case 'reconnecting':
        return 'Reconnecting...';
      default:
        return 'Live updates offline';
    }
  });

  readonly userMenu = computed<MenuItem[]>(() => {
    const session = this.session.session();
    return [
      {
        label: session ? `${session.email} - ${session.tenantName}` : '',
        disabled: true,
      },
      { separator: true },
      { label: 'Invite code', icon: 'pi pi-user-plus', command: () => this.inviteOpen.set(true) },
      { label: 'Sign out', icon: 'pi pi-sign-out', command: () => void this.auth.logout() },
    ];
  });
}
