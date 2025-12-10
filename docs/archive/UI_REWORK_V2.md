# TO2 UI Implementation Guide

## Confirmed Design Decisions

- **Navigation**: Collapsible sidebar
- **Landing**: Keep for unauthenticated users
- **Tournament Detail**: No-scroll with tab navigation for stages
- **Components**: PrimeNG only, no mixing
- **Tournament Formats**: Support all three backend formats:
  - **BracketOnly**: Direct single-elimination bracket (2-32 teams)
  - **GroupsAndBracket**: Round-robin groups followed by bracket playoffs (2-32 teams)
  - **GroupsOnly**: Round-robin groups without bracket stage (2-32 teams)

## Tournament State Flows by Format

### BracketOnly Format
```
Setup → SeedingBracket → BracketInProgress → Finished
```
- No group stage
- Goes directly to bracket after seeding
- Tournament finishes when bracket completes

### GroupsAndBracket Format
```
Setup → SeedingGroups → GroupsInProgress → GroupsCompleted
      → SeedingBracket → BracketInProgress → Finished
```
- Has both group stage and bracket playoffs
- Top teams from groups advance to bracket
- Tournament finishes when bracket completes

### GroupsOnly Format
```
Setup → SeedingGroups → GroupsInProgress → GroupsCompleted → Finished
```
- Only has group stage, no bracket
- Tournament finishes immediately after groups complete
- Final standings determined by group performance
- **No bracket tab should be shown** for this format

---

## 1. Project Structure

```
src/app/
├── core/
│   ├── services/
│   │   ├── auth.service.ts
│   │   ├── tournament.service.ts
│   │   ├── team.service.ts
│   │   ├── standing.service.ts
│   │   └── match.service.ts
│   ├── guards/
│   │   └── auth.guard.ts
│   ├── interceptors/
│   │   └── auth.interceptor.ts
│   └── models/
│       ├── tournament.model.ts
│       ├── team.model.ts
│       └── index.ts
│
├── shared/
│   ├── components/
│   │   ├── status-badge/
│   │   │   ├── status-badge.component.ts
│   │   │   ├── status-badge.component.html
│   │   │   └── status-badge.component.css
│   │   ├── team-chip/
│   │   ├── format-tag/
│   │   └── confirm-dialog/
│   ├── pipes/
│   │   └── tournament-status.pipe.ts
│   └── shared.module.ts
│
├── layouts/
│   ├── public-layout/
│   │   ├── public-layout.component.ts
│   │   ├── public-layout.component.html
│   │   └── public-layout.component.css
│   └── app-layout/
│       ├── app-layout.component.ts
│       ├── app-layout.component.html
│       ├── app-layout.component.css
│       └── sidebar/
│           ├── sidebar.component.ts
│           ├── sidebar.component.html
│           └── sidebar.component.css
│
├── features/
│   ├── landing/
│   │   ├── landing.component.ts
│   │   ├── landing.component.html
│   │   └── landing.component.css
│   │
│   ├── auth/
│   │   ├── login/
│   │   │   ├── login.component.ts
│   │   │   ├── login.component.html
│   │   │   └── login.component.css
│   │   └── register/
│   │       ├── register.component.ts
│   │       ├── register.component.html
│   │       └── register.component.css
│   │
│   ├── dashboard/
│   │   ├── dashboard.component.ts
│   │   ├── dashboard.component.html
│   │   └── dashboard.component.css
│   │
│   └── tournament/
│       ├── tournament-detail/
│       │   ├── tournament-detail.component.ts
│       │   ├── tournament-detail.component.html
│       │   └── tournament-detail.component.css
│       ├── create-wizard/
│       │   ├── create-wizard.component.ts
│       │   ├── create-wizard.component.html
│       │   └── create-wizard.component.css
│       ├── groups-panel/
│       │   ├── groups-panel.component.ts
│       │   ├── groups-panel.component.html
│       │   └── groups-panel.component.css
│       ├── bracket-panel/
│       │   ├── bracket-panel.component.ts
│       │   ├── bracket-panel.component.html
│       │   └── bracket-panel.component.css
│       └── teams-panel/
│           ├── teams-panel.component.ts
│           ├── teams-panel.component.html
│           └── teams-panel.component.css
│
├── app-routing.module.ts
├── app.module.ts
└── app.component.ts
```

---

## 2. Theme Configuration

### 2.1 Create `src/styles/theme.css`

```css
:root {
  /* Surfaces */
  --surface-ground: #0a0e14;
  --surface-card: #141a22;
  --surface-overlay: #1c242e;
  --surface-border: #2a3544;
  --surface-hover: #1e2832;

  /* Primary */
  --primary: #00d936;
  --primary-hover: #00ff3f;
  --primary-text: #0a0e14;

  /* Text */
  --text-primary: #e8eaed;
  --text-secondary: #8b949e;
  --text-muted: #525c68;

  /* Semantic */
  --danger: #ff4757;
  --warning: #ffa502;
  --info: #1e90ff;
  --success: #00d936;

  /* Shadows */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.3);
  --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.4);
  --shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.5);

  /* Spacing */
  --space-1: 0.25rem;
  --space-2: 0.5rem;
  --space-3: 0.75rem;
  --space-4: 1rem;
  --space-6: 1.5rem;
  --space-8: 2rem;

  /* Border radius */
  --radius-sm: 4px;
  --radius-md: 8px;
  --radius-lg: 12px;

  /* Sidebar */
  --sidebar-width-collapsed: 64px;
  --sidebar-width-expanded: 240px;

  /* Typography */
  --font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', monospace;
}

/* Base styles */
body {
  margin: 0;
  font-family: var(--font-family);
  background: var(--surface-ground);
  color: var(--text-primary);
  -webkit-font-smoothing: antialiased;
}

/* Scrollbar */
::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}
::-webkit-scrollbar-track {
  background: var(--surface-ground);
}
::-webkit-scrollbar-thumb {
  background: var(--surface-border);
  border-radius: 4px;
}
::-webkit-scrollbar-thumb:hover {
  background: var(--text-muted);
}
```

### 2.2 Create `src/styles/primeng-overrides.css`

```css
/* ===== BUTTONS ===== */
.p-button {
  background: var(--primary);
  border: 1px solid var(--primary);
  color: var(--primary-text);
  font-weight: 600;
  border-radius: var(--radius-md);
  transition: all 0.15s ease;
}
.p-button:enabled:hover {
  background: var(--primary-hover);
  border-color: var(--primary-hover);
}
.p-button.p-button-outlined {
  background: transparent;
  color: var(--primary);
  border: 1px solid var(--primary);
}
.p-button.p-button-outlined:enabled:hover {
  background: rgba(0, 217, 54, 0.1);
}
.p-button.p-button-text {
  background: transparent;
  border: none;
  color: var(--text-secondary);
}
.p-button.p-button-text:enabled:hover {
  background: var(--surface-hover);
  color: var(--text-primary);
}
.p-button.p-button-danger {
  background: var(--danger);
  border-color: var(--danger);
}

/* ===== INPUTS ===== */
.p-inputtext {
  background: var(--surface-ground);
  border: 1px solid var(--surface-border);
  color: var(--text-primary);
  border-radius: var(--radius-md);
  padding: 0.75rem 1rem;
}
.p-inputtext:enabled:focus {
  border-color: var(--primary);
  box-shadow: 0 0 0 2px rgba(0, 217, 54, 0.2);
}
.p-inputtext::placeholder {
  color: var(--text-muted);
}

/* ===== CARDS ===== */
.p-card {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-sm);
}
.p-card .p-card-title {
  color: var(--text-primary);
  font-weight: 600;
}
.p-card .p-card-content {
  color: var(--text-secondary);
}

/* ===== TABLE ===== */
.p-datatable .p-datatable-header {
  background: var(--surface-card);
  border-color: var(--surface-border);
}
.p-datatable .p-datatable-thead > tr > th {
  background: var(--surface-card);
  border-color: var(--surface-border);
  color: var(--text-secondary);
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.75rem;
  letter-spacing: 0.05em;
}
.p-datatable .p-datatable-tbody > tr {
  background: var(--surface-card);
  border-color: var(--surface-border);
  color: var(--text-primary);
}
.p-datatable .p-datatable-tbody > tr:hover {
  background: var(--surface-hover);
}

/* ===== DROPDOWN ===== */
.p-dropdown {
  background: var(--surface-ground);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
}
.p-dropdown:not(.p-disabled):hover {
  border-color: var(--primary);
}
.p-dropdown-panel {
  background: var(--surface-overlay);
  border: 1px solid var(--surface-border);
}
.p-dropdown-item {
  color: var(--text-primary);
}
.p-dropdown-item:hover {
  background: var(--surface-hover);
}

/* ===== TABVIEW ===== */
.p-tabview .p-tabview-nav {
  background: transparent;
  border: none;
  border-bottom: 1px solid var(--surface-border);
}
.p-tabview .p-tabview-nav li .p-tabview-nav-link {
  background: transparent;
  border: none;
  color: var(--text-secondary);
  padding: var(--space-3) var(--space-4);
}
.p-tabview .p-tabview-nav li.p-highlight .p-tabview-nav-link {
  color: var(--primary);
  border-bottom: 2px solid var(--primary);
}
.p-tabview .p-tabview-panels {
  background: transparent;
  padding: var(--space-4) 0;
}

/* ===== TAG ===== */
.p-tag {
  border-radius: var(--radius-sm);
  font-weight: 600;
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem;
}
.p-tag.p-tag-success {
  background: rgba(0, 217, 54, 0.15);
  color: var(--success);
}
.p-tag.p-tag-warning {
  background: rgba(255, 165, 2, 0.15);
  color: var(--warning);
}
.p-tag.p-tag-danger {
  background: rgba(255, 71, 87, 0.15);
  color: var(--danger);
}
.p-tag.p-tag-info {
  background: rgba(30, 144, 255, 0.15);
  color: var(--info);
}

/* ===== DIALOG ===== */
.p-dialog {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-lg);
}
.p-dialog .p-dialog-header {
  background: var(--surface-card);
  border-bottom: 1px solid var(--surface-border);
  color: var(--text-primary);
}
.p-dialog .p-dialog-content {
  background: var(--surface-card);
  color: var(--text-primary);
}

/* ===== PASSWORD ===== */
.p-password-input {
  width: 100%;
}

/* ===== TOAST ===== */
.p-toast .p-toast-message {
  background: var(--surface-overlay);
  border: 1px solid var(--surface-border);
}
.p-toast .p-toast-message-content {
  color: var(--text-primary);
}
```

### 2.3 Update `angular.json` styles array

```json
"styles": [
  "src/styles/theme.css",
  "src/styles/primeng-overrides.css",
  "node_modules/primeicons/primeicons.css",
  "src/styles.css"
]
```

---

## 3. Routing Configuration

### 3.1 Update `app-routing.module.ts`

```typescript
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

// Layouts
import { PublicLayoutComponent } from './layouts/public-layout/public-layout.component';
import { AppLayoutComponent } from './layouts/app-layout/app-layout.component';

// Features
import { LandingComponent } from './features/landing/landing.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { TournamentDetailComponent } from './features/tournament/tournament-detail/tournament-detail.component';
import { CreateWizardComponent } from './features/tournament/create-wizard/create-wizard.component';

const routes: Routes = [
  // Public routes (no sidebar)
  {
    path: '',
    component: PublicLayoutComponent,
    children: [
      { path: '', component: LandingComponent },
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent }
    ]
  },

  // Authenticated routes (with sidebar)
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'tournaments', redirectTo: 'dashboard' },
      { path: 'tournament/new', component: CreateWizardComponent },
      { path: 'tournament/:id', component: TournamentDetailComponent }
    ]
  },

  // Fallback
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
```

---

## 4. Layout Components

### 4.1 Public Layout

**public-layout.component.html**
```html
<div class="public-layout">
  <router-outlet></router-outlet>
</div>
```

**public-layout.component.css**
```css
.public-layout {
  min-height: 100vh;
  background: var(--surface-ground);
}
```

### 4.2 App Layout (with Sidebar)

**app-layout.component.ts**
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-layout',
  templateUrl: './app-layout.component.html',
  styleUrls: ['./app-layout.component.css']
})
export class AppLayoutComponent {
  sidebarCollapsed = false;

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }
}
```

**app-layout.component.html**
```html
<div class="app-layout" [class.sidebar-collapsed]="sidebarCollapsed">
  <app-sidebar 
    [collapsed]="sidebarCollapsed" 
    (toggle)="toggleSidebar()">
  </app-sidebar>
  
  <main class="main-content">
    <router-outlet></router-outlet>
  </main>
</div>
```

**app-layout.component.css**
```css
.app-layout {
  display: flex;
  min-height: 100vh;
  background: var(--surface-ground);
}

.main-content {
  flex: 1;
  margin-left: var(--sidebar-width-expanded);
  padding: var(--space-6);
  transition: margin-left 0.2s ease;
  overflow-y: auto;
  max-height: 100vh;
}

.app-layout.sidebar-collapsed .main-content {
  margin-left: var(--sidebar-width-collapsed);
}
```

### 4.3 Sidebar Component

**sidebar.component.ts**
```typescript
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent {
  @Input() collapsed = false;
  @Output() toggle = new EventEmitter<void>();

  navItems: NavItem[] = [
    { label: 'Tournaments', icon: 'pi-th-large', route: '/dashboard' }
  ];

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  get currentUser() {
    return this.authService.currentUserValue;
  }

  logout(): void {
    this.authService.logout();
  }

  onToggle(): void {
    this.toggle.emit();
  }
}
```

**sidebar.component.html**
```html
<aside class="sidebar" [class.collapsed]="collapsed">
  <!-- Logo -->
  <div class="sidebar-header">
    <div class="logo">
      <span class="logo-icon">⚔</span>
      <span class="logo-text" *ngIf="!collapsed">TO2</span>
    </div>
    <button pButton 
            icon="pi pi-bars" 
            class="p-button-text toggle-btn"
            (click)="onToggle()">
    </button>
  </div>

  <!-- Navigation -->
  <nav class="sidebar-nav">
    <a *ngFor="let item of navItems"
       [routerLink]="item.route"
       routerLinkActive="active"
       class="nav-item"
       [pTooltip]="collapsed ? item.label : null"
       tooltipPosition="right">
      <i class="pi" [ngClass]="item.icon"></i>
      <span class="nav-label" *ngIf="!collapsed">{{item.label}}</span>
    </a>
  </nav>

  <!-- Footer -->
  <div class="sidebar-footer">
    <div class="user-section" *ngIf="currentUser">
      <div class="user-info" *ngIf="!collapsed">
        <span class="user-name">{{currentUser.username}}</span>
        <span class="org-name">{{currentUser.tenantName}}</span>
      </div>
      <button pButton 
              [icon]="collapsed ? 'pi pi-sign-out' : ''"
              [label]="collapsed ? '' : 'Logout'"
              class="p-button-text p-button-danger logout-btn"
              (click)="logout()">
      </button>
    </div>
  </div>
</aside>
```

**sidebar.component.css**
```css
.sidebar {
  position: fixed;
  left: 0;
  top: 0;
  bottom: 0;
  width: var(--sidebar-width-expanded);
  background: var(--surface-card);
  border-right: 1px solid var(--surface-border);
  display: flex;
  flex-direction: column;
  transition: width 0.2s ease;
  z-index: 100;
}

.sidebar.collapsed {
  width: var(--sidebar-width-collapsed);
}

/* Header */
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-4);
  border-bottom: 1px solid var(--surface-border);
}

.logo {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.logo-icon {
  font-size: 1.5rem;
}

.logo-text {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--primary);
}

.toggle-btn {
  color: var(--text-secondary) !important;
}

/* Navigation */
.sidebar-nav {
  flex: 1;
  padding: var(--space-4) var(--space-2);
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.nav-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-3);
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  text-decoration: none;
  transition: all 0.15s ease;
}

.nav-item:hover {
  background: var(--surface-hover);
  color: var(--text-primary);
}

.nav-item.active {
  background: rgba(0, 217, 54, 0.1);
  color: var(--primary);
}

.nav-item i {
  font-size: 1.25rem;
  width: 24px;
  text-align: center;
}

.sidebar.collapsed .nav-item {
  justify-content: center;
  padding: var(--space-3);
}

.sidebar.collapsed .nav-label {
  display: none;
}

/* Footer */
.sidebar-footer {
  padding: var(--space-4);
  border-top: 1px solid var(--surface-border);
}

.user-section {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.user-info {
  flex: 1;
  min-width: 0;
}

.user-name {
  display: block;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.org-name {
  display: block;
  font-size: 0.75rem;
  color: var(--text-muted);
}

.logout-btn {
  flex-shrink: 0;
}

.sidebar.collapsed .user-info {
  display: none;
}

.sidebar.collapsed .user-section {
  justify-content: center;
}
```

---

## 5. Feature Components

### 5.1 Landing Page

**landing.component.html**
```html
<div class="landing">
  <!-- Hero Section -->
  <div class="hero">
    <div class="hero-content">
      <div class="logo-large">
        <span class="icon">⚔</span>
        <span class="text">TO2</span>
      </div>
      
      <h1>Tournament<br/>Command Center</h1>
      <p class="tagline">Brackets. Groups. Automation.</p>
      
      <div class="cta-buttons">
        <button pButton 
                label="Get Started" 
                icon="pi pi-arrow-right" 
                iconPos="right"
                routerLink="/register">
        </button>
        <button pButton 
                label="Sign In" 
                class="p-button-outlined"
                routerLink="/login">
        </button>
      </div>
      
      <div class="features-row">
        <div class="feature">
          <i class="pi pi-shield"></i>
          <span>Multi-tenant</span>
        </div>
        <div class="feature">
          <i class="pi pi-sitemap"></i>
          <span>Auto-brackets</span>
        </div>
        <div class="feature">
          <i class="pi pi-users"></i>
          <span>2-32 teams</span>
        </div>
      </div>
    </div>
    
    <div class="hero-visual">
      <!-- SVG bracket visualization -->
      <svg class="bracket-svg" viewBox="0 0 400 300">
        <!-- Simplified bracket lines with glow -->
        <defs>
          <filter id="glow">
            <feGaussianBlur stdDeviation="2" result="blur"/>
            <feMerge>
              <feMergeNode in="blur"/>
              <feMergeNode in="SourceGraphic"/>
            </feMerge>
          </filter>
        </defs>
        
        <g filter="url(#glow)" stroke="var(--primary)" stroke-width="2" fill="none">
          <!-- Round 1 -->
          <line x1="50" y1="40" x2="100" y2="40"/>
          <line x1="50" y1="80" x2="100" y2="80"/>
          <line x1="100" y1="40" x2="100" y2="80"/>
          <line x1="100" y1="60" x2="150" y2="60"/>
          
          <line x1="50" y1="140" x2="100" y2="140"/>
          <line x1="50" y1="180" x2="100" y2="180"/>
          <line x1="100" y1="140" x2="100" y2="180"/>
          <line x1="100" y1="160" x2="150" y2="160"/>
          
          <!-- Semis -->
          <line x1="150" y1="60" x2="150" y2="160"/>
          <line x1="150" y1="110" x2="200" y2="110"/>
          
          <!-- Final -->
          <line x1="200" y1="110" x2="250" y2="110"/>
          <circle cx="260" cy="110" r="8" fill="var(--primary)"/>
        </g>
      </svg>
    </div>
  </div>
</div>
```

**landing.component.css**
```css
.landing {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--surface-ground);
  padding: var(--space-8);
}

.hero {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-8);
  max-width: 1200px;
  width: 100%;
  align-items: center;
}

/* Content */
.hero-content {
  display: flex;
  flex-direction: column;
  gap: var(--space-6);
}

.logo-large {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.logo-large .icon {
  font-size: 2.5rem;
}

.logo-large .text {
  font-size: 2rem;
  font-weight: 800;
  color: var(--primary);
  letter-spacing: -0.02em;
}

h1 {
  font-size: 3.5rem;
  font-weight: 800;
  line-height: 1.1;
  color: var(--text-primary);
  margin: 0;
}

.tagline {
  font-size: 1.25rem;
  color: var(--text-secondary);
  margin: 0;
}

.cta-buttons {
  display: flex;
  gap: var(--space-4);
}

.cta-buttons .p-button {
  padding: var(--space-3) var(--space-6);
}

.features-row {
  display: flex;
  gap: var(--space-6);
  margin-top: var(--space-4);
}

.feature {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--text-muted);
  font-size: 0.875rem;
}

.feature i {
  color: var(--primary);
}

/* Visual */
.hero-visual {
  display: flex;
  align-items: center;
  justify-content: center;
}

.bracket-svg {
  width: 100%;
  max-width: 400px;
  opacity: 0.8;
}

@media (max-width: 900px) {
  .hero {
    grid-template-columns: 1fr;
    text-align: center;
  }
  
  .hero-content {
    align-items: center;
  }
  
  .hero-visual {
    order: -1;
  }
  
  h1 {
    font-size: 2.5rem;
  }
}
```

### 5.2 Login Component

**login.component.html**
```html
<div class="auth-layout">
  <div class="auth-brand">
    <div class="brand-content">
      <div class="logo">
        <span class="icon">⚔</span>
        <span class="text">TO2</span>
      </div>
      <p>Tournament management<br/>simplified.</p>
    </div>
  </div>
  
  <div class="auth-form-panel">
    <div class="auth-card">
      <h2>Sign In</h2>
      <p class="subtitle">Enter your credentials to continue</p>
      
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="field">
          <label for="email">Email</label>
          <span class="p-input-icon-left w-full">
            <i class="pi pi-envelope"></i>
            <input pInputText 
                   id="email" 
                   formControlName="email"
                   placeholder="Enter your email"
                   class="w-full" />
          </span>
        </div>
        
        <div class="field">
          <label for="password">Password</label>
          <p-password formControlName="password"
                      placeholder="Enter password"
                      [feedback]="false"
                      [toggleMask]="true"
                      styleClass="w-full">
          </p-password>
        </div>
        
        <button pButton 
                type="submit"
                label="Sign In"
                [loading]="isLoading"
                class="w-full">
        </button>
      </form>
      
      <p class="auth-switch">
        New here? <a routerLink="/register">Create account</a>
      </p>
    </div>
  </div>
</div>
```

**login.component.css**
```css
.auth-layout {
  min-height: 100vh;
  display: grid;
  grid-template-columns: 1fr 1fr;
}

/* Brand panel */
.auth-brand {
  background: var(--surface-card);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  position: relative;
  overflow: hidden;
}

.auth-brand::before {
  content: '';
  position: absolute;
  inset: 0;
  background: 
    radial-gradient(circle at 30% 70%, rgba(0, 217, 54, 0.05) 0%, transparent 50%),
    linear-gradient(135deg, transparent 40%, rgba(0, 217, 54, 0.03) 100%);
}

.brand-content {
  position: relative;
  text-align: center;
}

.brand-content .logo {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  margin-bottom: var(--space-6);
}

.brand-content .icon {
  font-size: 3rem;
}

.brand-content .text {
  font-size: 2.5rem;
  font-weight: 800;
  color: var(--primary);
}

.brand-content p {
  font-size: 1.25rem;
  color: var(--text-secondary);
  line-height: 1.5;
}

/* Form panel */
.auth-form-panel {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  background: var(--surface-ground);
}

.auth-card {
  width: 100%;
  max-width: 400px;
}

.auth-card h2 {
  margin: 0 0 var(--space-2);
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
}

.auth-card .subtitle {
  margin: 0 0 var(--space-6);
  color: var(--text-secondary);
}

.field {
  margin-bottom: var(--space-4);
}

.field label {
  display: block;
  margin-bottom: var(--space-2);
  font-weight: 500;
  color: var(--text-primary);
}

.auth-switch {
  text-align: center;
  margin-top: var(--space-6);
  color: var(--text-secondary);
}

.auth-switch a {
  color: var(--primary);
  text-decoration: none;
  font-weight: 600;
}

.auth-switch a:hover {
  text-decoration: underline;
}

@media (max-width: 768px) {
  .auth-layout {
    grid-template-columns: 1fr;
  }
  
  .auth-brand {
    display: none;
  }
}
```

### 5.3 Register Component

**register.component.html**
```html
<div class="auth-layout">
  <!-- Brand panel -->
  <div class="auth-brand">
    <div class="brand-content">
      <div class="logo">
        <span class="icon">⚔</span>
        <span class="text">TO2</span>
      </div>
      <p>Tournament management<br/>simplified.</p>
    </div>
  </div>

  <!-- Form panel -->
  <div class="auth-form-panel">
    <div class="auth-card">
      <h2>Create Account</h2>
      <p class="subtitle">Get started with TO2</p>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="field">
          <label for="username">Username</label>
          <span class="p-input-icon-left w-full">
            <i class="pi pi-user"></i>
            <input pInputText
                   id="username"
                   formControlName="username"
                   placeholder="Choose a username"
                   class="w-full" />
          </span>
        </div>

        <div class="field">
          <label for="email">Email</label>
          <span class="p-input-icon-left w-full">
            <i class="pi pi-envelope"></i>
            <input pInputText
                   id="email"
                   formControlName="email"
                   type="email"
                   placeholder="Enter your email"
                   class="w-full" />
          </span>
        </div>

        <div class="field">
          <label for="password">Password</label>
          <p-password formControlName="password"
                      placeholder="Create password"
                      [feedback]="false"
                      [toggleMask]="true"
                      styleClass="w-full">
          </p-password>
        </div>

        <div class="field">
          <label for="tenantName">Organization Name</label>
          <span class="p-input-icon-left w-full">
            <i class="pi pi-building"></i>
            <input pInputText
                   id="tenantName"
                   formControlName="tenantName"
                   placeholder="Your organization name"
                   class="w-full" />
          </span>
        </div>

        <button pButton
                type="submit"
                label="Create Account"
                [loading]="isLoading"
                class="w-full">
        </button>
      </form>

      <p class="auth-switch">
        Already have an account? <a routerLink="/login">Sign in</a>
      </p>
    </div>
  </div>
</div>
```

**register.component.css**
```css
/* Reuse all auth layout styles from login.component.css */
.auth-layout {
  min-height: 100vh;
  display: grid;
  grid-template-columns: 1fr 1fr;
}

/* Brand panel */
.auth-brand {
  background: var(--surface-card);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  position: relative;
  overflow: hidden;
}

.auth-brand::before {
  content: '';
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 30% 70%, rgba(0, 217, 54, 0.05) 0%, transparent 50%),
    linear-gradient(135deg, transparent 40%, rgba(0, 217, 54, 0.03) 100%);
}

.brand-content {
  position: relative;
  text-align: center;
}

.brand-content .logo {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  margin-bottom: var(--space-6);
}

.brand-content .icon {
  font-size: 3rem;
}

.brand-content .text {
  font-size: 2.5rem;
  font-weight: 800;
  color: var(--primary);
}

.brand-content p {
  font-size: 1.25rem;
  color: var(--text-secondary);
  line-height: 1.5;
}

/* Form panel */
.auth-form-panel {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  background: var(--surface-ground);
}

.auth-card {
  width: 100%;
  max-width: 400px;
}

.auth-card h2 {
  margin: 0 0 var(--space-2);
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
}

.auth-card .subtitle {
  margin: 0 0 var(--space-6);
  color: var(--text-secondary);
}

.field {
  margin-bottom: var(--space-4);
}

.field label {
  display: block;
  margin-bottom: var(--space-2);
  font-weight: 500;
  color: var(--text-primary);
}

.auth-switch {
  text-align: center;
  margin-top: var(--space-6);
  color: var(--text-secondary);
}

.auth-switch a {
  color: var(--primary);
  text-decoration: none;
  font-weight: 600;
}

.auth-switch a:hover {
  text-decoration: underline;
}

@media (max-width: 768px) {
  .auth-layout {
    grid-template-columns: 1fr;
  }

  .auth-brand {
    display: none;
  }
}
```

**register.component.ts** (Key Logic)
```typescript
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  form: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private messageService: MessageService
  ) {
    this.form = this.fb.group({
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      tenantName: ['', Validators.required]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const { username, email, password, tenantName } = this.form.value;

    this.authService.register(username, email, password, tenantName).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Account created successfully'
        });
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Registration Failed',
          detail: error.error?.detail || 'Failed to create account'
        });
        this.isLoading = false;
      }
    });
  }
}
```

### 5.4 Dashboard (Tournament List)

**dashboard.component.html**
```html
<div class="dashboard">
  <!-- Header -->
  <header class="page-header">
    <h1>Tournaments</h1>
    <button pButton 
            label="New Tournament" 
            icon="pi pi-plus"
            routerLink="/tournament/new">
    </button>
  </header>
  
  <!-- Stats -->
  <div class="stats-row">
    <div class="stat-card">
      <span class="stat-value">{{activeCount}}</span>
      <span class="stat-label">Active</span>
    </div>
    <div class="stat-card">
      <span class="stat-value">{{completedCount}}</span>
      <span class="stat-label">Completed</span>
    </div>
    <div class="stat-card">
      <span class="stat-value">{{totalTeams}}</span>
      <span class="stat-label">Teams</span>
    </div>
  </div>
  
  <!-- Table -->
  <p-table [value]="tournaments" 
           [loading]="isLoading"
           styleClass="p-datatable-sm"
           [rowHover]="true">
    <ng-template pTemplate="header">
      <tr>
        <th>Tournament</th>
        <th style="width: 140px">Format</th>
        <th style="width: 100px">Teams</th>
        <th style="width: 140px">Status</th>
        <th style="width: 100px">Actions</th>
      </tr>
    </ng-template>
    
    <ng-template pTemplate="body" let-t>
      <tr>
        <td>
          <div class="tournament-cell">
            <span class="name">{{t.name}}</span>
            <span class="desc">{{t.description}}</span>
          </div>
        </td>
        <td>
          <app-format-tag [format]="t.format"></app-format-tag>
        </td>
        <td>
          <span class="teams-count">{{t.teamCount}}/{{t.maxTeams}}</span>
        </td>
        <td>
          <app-status-badge [status]="t.status"></app-status-badge>
        </td>
        <td>
          <button pButton 
                  icon="pi pi-eye" 
                  class="p-button-text p-button-sm"
                  [routerLink]="['/tournament', t.id]"
                  pTooltip="View">
          </button>
        </td>
      </tr>
    </ng-template>
    
    <ng-template pTemplate="emptymessage">
      <tr>
        <td colspan="5" class="empty-state">
          <i class="pi pi-inbox"></i>
          <p>No tournaments yet</p>
          <button pButton 
                  label="Create your first tournament"
                  class="p-button-outlined"
                  routerLink="/tournament/new">
          </button>
        </td>
      </tr>
    </ng-template>
  </p-table>
</div>
```

**dashboard.component.css**
```css
.dashboard {
  max-width: 1200px;
}

/* Header */
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-6);
}

.page-header h1 {
  margin: 0;
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
}

/* Stats */
.stats-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-4);
  margin-bottom: var(--space-6);
}

.stat-card {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
  padding: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.stat-value {
  font-size: 2rem;
  font-weight: 700;
  color: var(--primary);
}

.stat-label {
  font-size: 0.875rem;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* Table cells */
.tournament-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.tournament-cell .name {
  font-weight: 600;
  color: var(--text-primary);
}

.tournament-cell .desc {
  font-size: 0.875rem;
  color: var(--text-muted);
}

.teams-count {
  font-weight: 600;
  font-family: var(--font-mono);
}

/* Empty state */
.empty-state {
  text-align: center;
  padding: var(--space-8) !important;
}

.empty-state i {
  font-size: 3rem;
  color: var(--text-muted);
  margin-bottom: var(--space-4);
}

.empty-state p {
  color: var(--text-secondary);
  margin-bottom: var(--space-4);
}
```

### 5.4 Tournament Detail

**tournament-detail.component.html**
```html
<div class="tournament-detail" *ngIf="tournament">
  <!-- Header Row -->
  <header class="detail-header">
    <div class="header-left">
      <button pButton 
              icon="pi pi-arrow-left" 
              class="p-button-text"
              routerLink="/dashboard">
      </button>
      <div class="title-group">
        <div class="badges">
          <app-status-badge [status]="tournament.status"></app-status-badge>
          <app-format-tag [format]="tournament.format"></app-format-tag>
        </div>
        <h1>{{tournament.name}}</h1>
      </div>
    </div>
    <div class="header-actions">
      <button pButton 
              *ngIf="canAddTeams"
              label="Add Teams" 
              icon="pi pi-plus"
              class="p-button-outlined"
              (click)="showAddTeamsDialog()">
      </button>
      <button pButton 
              *ngIf="canStartGroups"
              label="Start Groups" 
              icon="pi pi-play"
              (click)="startGroups()">
      </button>
      <button pButton 
              *ngIf="canStartBracket"
              label="Start Bracket" 
              icon="pi pi-play"
              (click)="startBracket()">
      </button>
    </div>
  </header>

  <!-- Main Content Grid -->
  <div class="detail-grid">
    <!-- Left: Info Panel -->
    <aside class="info-panel">
      <!-- Config -->
      <div class="info-card">
        <h3>Configuration</h3>
        <div class="config-grid">
          <div class="config-item">
            <span class="label">Teams</span>
            <span class="value">{{tournament.teamCount}}/{{tournament.maxTeams}}</span>
          </div>
          <div class="config-item" *ngIf="tournament.teamsPerGroup">
            <span class="label">Per Group</span>
            <span class="value">{{tournament.teamsPerGroup}}</span>
          </div>
          <div class="config-item" *ngIf="tournament.teamsPerBracket">
            <span class="label">Bracket Size</span>
            <span class="value">{{tournament.teamsPerBracket}}</span>
          </div>
        </div>
      </div>
      
      <!-- Teams -->
      <div class="info-card">
        <h3>Teams <span class="count">({{teams.length}})</span></h3>
        <div class="teams-list">
          <div class="team-chip" *ngFor="let team of teams; let i = index">
            <span class="seed">{{i + 1}}</span>
            <span class="name">{{team.name}}</span>
          </div>
        </div>
      </div>
      
      <!-- Results (when finished) -->
      <div class="info-card" *ngIf="tournament.status === 'Finished'">
        <h3>Results</h3>
        <div class="podium">
          <div class="place" *ngFor="let standing of topResults">
            <span class="medal" [ngClass]="'place-' + standing.position">
              {{standing.position}}
            </span>
            <span class="team-name">{{standing.teamName}}</span>
          </div>
        </div>
      </div>
    </aside>

    <!-- Center: Stage View -->
    <section class="stage-panel">
      <p-tabView [(activeIndex)]="activeTabIndex">
        <!-- Groups Tab -->
        <p-tabPanel header="Groups" *ngIf="hasGroups">
          <app-groups-panel 
            [groups]="groups"
            [matches]="groupMatches"
            (matchResult)="onMatchResult($event)">
          </app-groups-panel>
        </p-tabPanel>
        
        <!-- Bracket Tab -->
        <p-tabPanel header="Bracket" *ngIf="hasBracket">
          <app-bracket-panel
            [bracket]="bracket"
            [matches]="bracketMatches"
            (matchResult)="onMatchResult($event)">
          </app-bracket-panel>
        </p-tabPanel>
      </p-tabView>
    </section>
  </div>
</div>

<!-- Add Teams Dialog -->
<p-dialog header="Add Teams" 
          [(visible)]="showTeamDialog"
          [modal]="true"
          [style]="{width: '500px'}">
  <app-teams-panel
    [availableTeams]="availableTeams"
    [maxTeams]="(tournament?.maxTeams ?? 0) - (tournament?.teamCount ?? 0)"
    (teamsSelected)="onTeamsAdded($event)">
  </app-teams-panel>
</p-dialog>
```

**tournament-detail.component.css**
```css
.tournament-detail {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 48px); /* Account for layout padding */
  overflow: hidden;
}

/* Header */
.detail-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: var(--space-4);
  border-bottom: 1px solid var(--surface-border);
  margin-bottom: var(--space-4);
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.title-group {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.badges {
  display: flex;
  gap: var(--space-2);
}

.title-group h1 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
}

.header-actions {
  display: flex;
  gap: var(--space-3);
}

/* Main Grid */
.detail-grid {
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: var(--space-4);
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

/* Info Panel */
.info-panel {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  overflow-y: auto;
}

.info-card {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
  padding: var(--space-4);
}

.info-card h3 {
  margin: 0 0 var(--space-3);
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.info-card h3 .count {
  color: var(--text-muted);
  font-weight: 400;
}

/* Config grid */
.config-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
}

.config-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.config-item .label {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.config-item .value {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  font-family: var(--font-mono);
}

/* Teams list */
.teams-list {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.team-chip {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  background: var(--surface-ground);
  border-radius: var(--radius-sm);
  padding: var(--space-1) var(--space-2);
  font-size: 0.875rem;
}

.team-chip .seed {
  width: 20px;
  height: 20px;
  background: var(--primary);
  color: var(--surface-ground);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 700;
}

.team-chip .name {
  color: var(--text-primary);
}

/* Podium */
.podium {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.place {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.place .medal {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 0.875rem;
  /* Default styling for 4+ places */
  background: var(--surface-border);
  color: var(--text-secondary);
}

.place .medal.place-1 {
  background: #ffd700;
  color: #000;
}

.place .medal.place-2 {
  background: #c0c0c0;
  color: #000;
}

.place .medal.place-3 {
  background: #cd7f32;
  color: #000;
}

.place .team-name {
  font-weight: 600;
  color: var(--text-primary);
}

/* Stage Panel */
.stage-panel {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.stage-panel ::ng-deep .p-tabview {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.stage-panel ::ng-deep .p-tabview-panels {
  flex: 1;
  overflow-y: auto;
}
```

**tournament-detail.component.ts** (Key Logic)
```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TournamentService } from '../../../core/services/tournament.service';
import { Format, TournamentStatus } from '../../../core/models/tournament.model';

@Component({
  selector: 'app-tournament-detail',
  templateUrl: './tournament-detail.component.html',
  styleUrls: ['./tournament-detail.component.css']
})
export class TournamentDetailComponent implements OnInit {
  tournament: any;
  teams: any[] = [];
  groups: any[] = [];
  bracket: any;
  groupMatches: any[] = [];
  bracketMatches: any[] = [];
  activeTabIndex = 0;
  showTeamDialog = false;
  availableTeams: any[] = [];
  topResults: any[] = [];

  constructor(
    private route: ActivatedRoute,
    private tournamentService: TournamentService
  ) {}

  ngOnInit(): void {
    // Load tournament data
    const id = this.route.snapshot.paramMap.get('id');
    // ... load tournament, teams, matches, etc.
  }

  // Tab Visibility Logic
  get hasGroups(): boolean {
    if (!this.tournament) return false;
    return this.tournament.format === Format.GroupsAndBracket ||
           this.tournament.format === Format.GroupsOnly;
  }

  get hasBracket(): boolean {
    if (!this.tournament) return false;
    return this.tournament.format === Format.BracketOnly ||
           this.tournament.format === Format.GroupsAndBracket;
  }

  // Button Visibility Logic
  get canAddTeams(): boolean {
    if (!this.tournament) return false;
    return this.tournament.status === TournamentStatus.Setup;
  }

  get canStartGroups(): boolean {
    if (!this.tournament) return false;
    return this.tournament.status === TournamentStatus.SeedingGroups &&
           this.tournament.format !== Format.BracketOnly;
  }

  get canStartBracket(): boolean {
    if (!this.tournament) return false;
    return (this.tournament.status === TournamentStatus.GroupsCompleted &&
            this.tournament.format === Format.GroupsAndBracket) ||
           (this.tournament.status === TournamentStatus.SeedingBracket &&
            this.tournament.format === Format.BracketOnly);
  }

  // Action Methods
  showAddTeamsDialog(): void {
    // Load available teams and show dialog
    this.showTeamDialog = true;
  }

  startGroups(): void {
    // Call service to start groups
    this.tournamentService.startGroups(this.tournament.id).subscribe();
  }

  startBracket(): void {
    // Call service to start bracket
    this.tournamentService.startBracket(this.tournament.id).subscribe();
  }

  onMatchResult(event: any): void {
    // Handle match result submission
  }

  onTeamsAdded(teams: any[]): void {
    // Handle teams added to tournament
    this.showTeamDialog = false;
  }
}
```

### 5.6 Create Tournament

**create-tournament.component.html**
```html
<div class="create-tournament-container">
  <div class="create-form-card">
    <h1>Create Tournament</h1>
    <p class="subtitle">Set up a new tournament in minutes</p>

    <form [formGroup]="form" (ngSubmit)="onSubmit()">
      <!-- Basic Information -->
      <div class="section-header">Basic Information</div>

      <div class="field">
        <label for="name">Tournament Name *</label>
        <input pInputText
               id="name"
               formControlName="name"
               placeholder="Enter tournament name"
               class="w-full" />
        <small class="field-hint">4-100 characters</small>
      </div>

      <div class="field">
        <label for="description">Description</label>
        <textarea pInputTextarea
                  id="description"
                  formControlName="description"
                  placeholder="Add a description (optional)"
                  rows="3"
                  class="w-full">
        </textarea>
        <small class="field-hint">Maximum 250 characters</small>
      </div>

      <!-- Tournament Format -->
      <div class="section-header">Tournament Format</div>

      <div class="format-selection">
        <div class="format-card"
             [class.selected]="form.get('format')?.value === Format.BracketOnly"
             (click)="selectFormat(Format.BracketOnly)">
          <i class="pi pi-sitemap"></i>
          <h3>Bracket Only</h3>
          <p>Direct single-elimination bracket</p>
        </div>

        <div class="format-card"
             [class.selected]="form.get('format')?.value === Format.GroupsAndBracket"
             (click)="selectFormat(Format.GroupsAndBracket)">
          <i class="pi pi-th-large"></i>
          <h3>Groups + Bracket</h3>
          <p>Round-robin groups then playoffs</p>
        </div>

        <div class="format-card"
             [class.selected]="form.get('format')?.value === Format.GroupsOnly"
             (click)="selectFormat(Format.GroupsOnly)">
          <i class="pi pi-list"></i>
          <h3>Groups Only</h3>
          <p>Round-robin groups, no bracket</p>
        </div>
      </div>

      <!-- Team Configuration -->
      <div class="section-header">Team Configuration</div>

      <div class="field">
        <label for="maxTeams">Max Teams *</label>
        <p-dropdown id="maxTeams"
                    formControlName="maxTeams"
                    [options]="maxTeamsOptions"
                    placeholder="Select max teams"
                    styleClass="w-full">
        </p-dropdown>
        <small class="field-hint" *ngIf="form.get('format')?.value === Format.BracketOnly">
          Must be a power of 2 (2, 4, 8, 16, 32)
        </small>
        <small class="field-hint" *ngIf="showTeamsPerGroup">
          Must be divisible by teams per group
        </small>
      </div>

      <div class="field" *ngIf="showTeamsPerGroup">
        <label for="teamsPerGroup">Teams Per Group *</label>
        <p-dropdown id="teamsPerGroup"
                    formControlName="teamsPerGroup"
                    [options]="teamsPerGroupOptions"
                    placeholder="Select teams per group"
                    styleClass="w-full">
        </p-dropdown>
        <small class="field-hint">2-16 teams per group</small>
      </div>

      <div class="field" *ngIf="showTeamsPerBracket">
        <label for="teamsPerBracket">Bracket Size *</label>
        <p-dropdown id="teamsPerBracket"
                    formControlName="teamsPerBracket"
                    [options]="teamsPerBracketOptions"
                    placeholder="Select bracket size"
                    styleClass="w-full">
        </p-dropdown>
        <small class="field-hint">Number of teams advancing to bracket playoffs</small>
      </div>

      <!-- Action Buttons -->
      <div class="form-actions">
        <button pButton
                type="button"
                label="Cancel"
                class="p-button-outlined"
                routerLink="/dashboard">
        </button>
        <button pButton
                type="submit"
                label="Create Tournament"
                icon="pi pi-arrow-right"
                iconPos="right"
                [loading]="isSubmitting"
                [disabled]="form.invalid">
        </button>
      </div>
    </form>
  </div>
</div>
```

**create-tournament.component.css**
```css
.create-tournament-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  background: var(--surface-ground);
}

.create-form-card {
  width: 100%;
  max-width: 600px;
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-lg);
  padding: var(--space-8);
}

.create-form-card h1 {
  margin: 0 0 var(--space-2);
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
}

.create-form-card .subtitle {
  margin: 0 0 var(--space-6);
  color: var(--text-secondary);
}

/* Section Headers */
.section-header {
  margin: var(--space-6) 0 var(--space-4);
  padding-bottom: var(--space-2);
  border-bottom: 1px solid var(--surface-border);
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--text-secondary);
}

.section-header:first-of-type {
  margin-top: 0;
}

/* Fields */
.field {
  margin-bottom: var(--space-4);
}

.field label {
  display: block;
  margin-bottom: var(--space-2);
  font-weight: 500;
  color: var(--text-primary);
}

.field-hint {
  display: block;
  margin-top: var(--space-1);
  font-size: 0.75rem;
  color: var(--text-muted);
}

/* Format Selection */
.format-selection {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-4);
  margin-bottom: var(--space-4);
}

.format-card {
  padding: var(--space-4);
  border: 2px solid var(--surface-border);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
  text-align: center;
  background: var(--surface-ground);
}

.format-card:hover {
  border-color: rgba(0, 217, 54, 0.5);
  background: rgba(0, 217, 54, 0.02);
}

.format-card.selected {
  border-color: var(--primary);
  background: rgba(0, 217, 54, 0.05);
}

.format-card i {
  font-size: 2rem;
  color: var(--text-secondary);
  margin-bottom: var(--space-3);
}

.format-card.selected i {
  color: var(--primary);
}

.format-card h3 {
  margin: 0 0 var(--space-2);
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.format-card p {
  margin: 0;
  font-size: 0.875rem;
  color: var(--text-muted);
  line-height: 1.4;
}

/* Form Actions */
.form-actions {
  display: flex;
  gap: var(--space-3);
  justify-content: flex-end;
  margin-top: var(--space-6);
  padding-top: var(--space-6);
  border-top: 1px solid var(--surface-border);
}

/* Responsive */
@media (max-width: 768px) {
  .create-tournament-container {
    padding: var(--space-4);
  }

  .create-form-card {
    padding: var(--space-6);
  }

  .format-selection {
    grid-template-columns: 1fr;
  }

  .form-actions {
    flex-direction: column-reverse;
  }

  .form-actions button {
    width: 100%;
  }
}
```

**create-tournament.component.ts**
```typescript
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { TournamentService } from '../../../core/services/tournament.service';
import { MessageService } from 'primeng/api';
import { Format } from '../../../core/models/tournament.model';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrls: ['./create-tournament.component.css']
})
export class CreateTournamentComponent implements OnInit {
  form: FormGroup;
  isSubmitting = false;
  Format = Format; // Expose enum to template

  maxTeamsOptions = [2, 4, 8, 16, 32].map(n => ({ label: `${n} teams`, value: n }));
  teamsPerGroupOptions = [2, 4, 8, 16].map(n => ({ label: `${n} teams`, value: n }));
  teamsPerBracketOptions = [4, 8, 16, 32].map(n => ({ label: `${n} teams`, value: n }));

  constructor(
    private fb: FormBuilder,
    private tournamentService: TournamentService,
    private router: Router,
    private messageService: MessageService
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(250)]],
      format: [Format.BracketOnly, Validators.required],
      maxTeams: [8, Validators.required],
      teamsPerGroup: [null],
      teamsPerBracket: [null]
    });
  }

  ngOnInit(): void {
    // Watch format changes to update validators
    this.form.get('format')?.valueChanges.subscribe(format => {
      this.updateValidatorsForFormat(format);
    });

    // Initialize validators for default format
    this.updateValidatorsForFormat(Format.BracketOnly);
  }

  get showTeamsPerGroup(): boolean {
    const format = this.form.get('format')?.value;
    return format === Format.GroupsAndBracket || format === Format.GroupsOnly;
  }

  get showTeamsPerBracket(): boolean {
    const format = this.form.get('format')?.value;
    return format === Format.GroupsAndBracket;
  }

  selectFormat(format: Format): void {
    this.form.patchValue({ format });
  }

  updateValidatorsForFormat(format: Format): void {
    const maxTeamsControl = this.form.get('maxTeams');
    const teamsPerGroupControl = this.form.get('teamsPerGroup');
    const teamsPerBracketControl = this.form.get('teamsPerBracket');

    // Clear all validators first
    maxTeamsControl?.clearValidators();
    teamsPerGroupControl?.clearValidators();
    teamsPerBracketControl?.clearValidators();

    // Set validators based on format
    if (format === Format.BracketOnly) {
      maxTeamsControl?.setValidators([Validators.required, this.powerOfTwoValidator]);
      teamsPerGroupControl?.setValue(null);
      teamsPerBracketControl?.setValue(null);
    } else if (format === Format.GroupsAndBracket) {
      maxTeamsControl?.setValidators([Validators.required, Validators.min(2), Validators.max(32)]);
      teamsPerGroupControl?.setValidators([Validators.required]);
      teamsPerBracketControl?.setValidators([Validators.required]);
    } else if (format === Format.GroupsOnly) {
      maxTeamsControl?.setValidators([Validators.required, Validators.min(2), Validators.max(32)]);
      teamsPerGroupControl?.setValidators([Validators.required]);
      teamsPerBracketControl?.setValue(null);
    }

    // Update validity
    maxTeamsControl?.updateValueAndValidity();
    teamsPerGroupControl?.updateValueAndValidity();
    teamsPerBracketControl?.updateValueAndValidity();
  }

  powerOfTwoValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;
    const powerOfTwo = [2, 4, 8, 16, 32];
    return powerOfTwo.includes(value) ? null : { powerOfTwo: true };
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const tournament = this.form.value;

    // Auto-set teamsPerBracket for BracketOnly
    if (tournament.format === Format.BracketOnly) {
      tournament.teamsPerBracket = tournament.maxTeams;
    }

    this.tournamentService.createTournament(tournament).subscribe({
      next: (created) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Tournament created successfully'
        });
        this.router.navigate(['/tournament', created.id]);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.detail || 'Failed to create tournament'
        });
        this.isSubmitting = false;
      }
    });
  }
}
```

### 5.7 Shared Components

**status-badge.component.ts**
```typescript
import { Component, Input } from '@angular/core';
import { TournamentStatus } from '../../../core/models/tournament.model';

@Component({
  selector: 'app-status-badge',
  template: `
    <span class="status-badge" [ngClass]="statusClass">
      {{displayText}}
    </span>
  `,
  styles: [`
    .status-badge {
      display: inline-flex;
      align-items: center;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }
    .status-setup {
      background: rgba(30, 144, 255, 0.15);
      color: #1e90ff;
    }
    .status-groups {
      background: rgba(255, 165, 2, 0.15);
      color: #ffa502;
    }
    .status-bracket {
      background: rgba(0, 217, 54, 0.15);
      color: #00d936;
    }
    .status-finished {
      background: rgba(139, 148, 158, 0.15);
      color: #8b949e;
    }
    .status-cancelled {
      background: rgba(255, 71, 87, 0.15);
      color: #ff4757;
    }
  `]
})
export class StatusBadgeComponent {
  @Input() status!: TournamentStatus;

  get statusClass(): string {
    const map: Record<TournamentStatus, string> = {
      [TournamentStatus.Setup]: 'status-setup',
      [TournamentStatus.SeedingGroups]: 'status-groups',
      [TournamentStatus.GroupsInProgress]: 'status-groups',
      [TournamentStatus.GroupsCompleted]: 'status-groups',
      [TournamentStatus.SeedingBracket]: 'status-bracket',
      [TournamentStatus.BracketInProgress]: 'status-bracket',
      [TournamentStatus.Finished]: 'status-finished',
      [TournamentStatus.Cancelled]: 'status-cancelled'
    };
    return map[this.status] || '';
  }

  get displayText(): string {
    const map: Record<TournamentStatus, string> = {
      [TournamentStatus.Setup]: 'Setup',
      [TournamentStatus.SeedingGroups]: 'Seeding',
      [TournamentStatus.GroupsInProgress]: 'Groups',
      [TournamentStatus.GroupsCompleted]: 'Groups Done',
      [TournamentStatus.SeedingBracket]: 'Seeding',
      [TournamentStatus.BracketInProgress]: 'Bracket',
      [TournamentStatus.Finished]: 'Finished',
      [TournamentStatus.Cancelled]: 'Cancelled'
    };
    return map[this.status] || this.status;
  }
}
```

**format-tag.component.ts**
```typescript
import { Component, Input } from '@angular/core';
import { Format } from '../../../core/models/tournament.model';

@Component({
  selector: 'app-format-tag',
  template: `
    <p-tag [value]="displayText" [severity]="severity"></p-tag>
  `
})
export class FormatTagComponent {
  @Input() format!: Format;

  get displayText(): string {
    const map: Record<Format, string> = {
      [Format.BracketOnly]: 'Bracket',
      [Format.GroupsAndBracket]: 'Groups + Bracket',
      [Format.GroupsOnly]: 'Groups Only'
    };
    return map[this.format] || this.format;
  }

  get severity(): string {
    const severityMap: Record<Format, string> = {
      [Format.BracketOnly]: 'danger',
      [Format.GroupsAndBracket]: 'success',
      [Format.GroupsOnly]: 'info'
    };
    return severityMap[this.format] || 'info';
  }
}
```

---

## 6. Implementation Phases

### Phase 1: Foundation (Day 1-2)

1. Create theme files (`theme.css`, `primeng-overrides.css`)
2. Update `angular.json` styles
3. Create layout components:
   - `PublicLayoutComponent`
   - `AppLayoutComponent`
   - `SidebarComponent`
4. Update routing structure
5. Create shared components:
   - `StatusBadgeComponent`
   - `FormatTagComponent`

**Verification:** Sidebar renders, layouts switch based on auth

### Phase 2: Auth & Landing (Day 2-3)

1. Redesign `LandingComponent`
2. Redesign `LoginComponent`
3. Redesign `RegisterComponent`
4. Test auth flow end-to-end

**Verification:** Complete unauthenticated user flow works

### Phase 3: Dashboard (Day 3-4)

1. Create `DashboardComponent` with table
2. Add stats row
3. Add empty state
4. Wire up to existing services

**Verification:** Tournament list displays in table format

### Phase 4: Tournament Detail (Day 4-6)

1. Create `TournamentDetailComponent` layout
2. Create `GroupsPanelComponent`
3. Create `BracketPanelComponent`
4. Create `TeamsPanelComponent`
5. Integrate existing match reporting
6. Add action buttons with state logic

**Verification:** Full tournament lifecycle manageable

### Phase 5: Create Tournament Form (Day 6-7)

1. Implement `CreateTournamentComponent` as single-page form
2. Format selection with visual cards
3. Dynamic field validation based on format
4. Conditional field display (teams per group/bracket)
5. Custom validators (power of 2, divisibility)

**Verification:** Tournament creation works with all three formats

### Phase 6: Polish (Day 7-8)

1. Loading states
2. Error handling toasts
3. Animations/transitions
4. Responsive breakpoints
5. Remove old components

---

## 7. PrimeNG Components Reference

| Component | Import | Usage |
|-----------|--------|-------|
| Button | `ButtonModule` | All buttons |
| InputText | `InputTextModule` | Text inputs |
| Password | `PasswordModule` | Password fields |
| Dropdown | `DropdownModule` | Selects |
| Table | `TableModule` | Tournament list |
| TabView | `TabViewModule` | Stage switching |
| Dialog | `DialogModule` | Modals |
| Tag | `TagModule` | Format badges |
| Toast | `ToastModule` | Notifications |
| Tooltip | `TooltipModule` | Hover hints |
| ProgressSpinner | `ProgressSpinnerModule` | Loading |
| Card | `CardModule` | Info cards |

---

## 8. Migration Checklist

### Components to Deprecate

- [ ] `NavbarComponent` → Replaced by `SidebarComponent`
- [ ] `TournamentListComponent` → Replaced by `DashboardComponent`
- [ ] `TournamentHeaderComponent` → Merged into `TournamentDetailComponent`
- [ ] `TournamentStateBannerComponent` → Replaced by `StatusBadgeComponent`
- [ ] `TournamentInfoCardComponent` → Merged into detail layout
- [ ] `TopResultsCardComponent` → Merged into info panel
- [ ] `RegisteredTeamsListComponent` → Replaced by teams list in info panel

### Services (No Changes)

All existing services remain unchanged:
- `AuthService`
- `TournamentService`
- `TeamService`
- `StandingService`
- `MatchService`

### Models (No Changes)

All existing models remain unchanged.

---

## 9. File Checklist

```
[ ] src/styles/theme.css
[ ] src/styles/primeng-overrides.css
[ ] angular.json (update styles array)
[ ] src/app/app-routing.module.ts
[ ] src/app/layouts/public-layout/public-layout.component.*
[ ] src/app/layouts/app-layout/app-layout.component.*
[ ] src/app/layouts/app-layout/sidebar/sidebar.component.*
[ ] src/app/shared/components/status-badge/status-badge.component.ts
[ ] src/app/shared/components/format-tag/format-tag.component.ts
[ ] src/app/shared/shared.module.ts
[ ] src/app/features/landing/landing.component.*
[ ] src/app/features/auth/login/login.component.*
[ ] src/app/features/auth/register/register.component.*
[ ] src/app/features/dashboard/dashboard.component.*
[ ] src/app/features/tournament/tournament-detail/tournament-detail.component.*
[ ] src/app/features/tournament/groups-panel/groups-panel.component.*
[ ] src/app/features/tournament/bracket-panel/bracket-panel.component.*
[ ] src/app/features/tournament/teams-panel/teams-panel.component.*
[ ] src/app/features/tournament/create-tournament/create-tournament.component.*
```

# TO2 UI Implementation Guide - V3 (Complete)

> **Note for Junior Developer:** This document builds upon UI_REWORK_V2.md. 
> Sections 1-5 remain unchanged. This expansion adds detailed specifications 
> for Groups Panel, Bracket Panel, and Results display.

---

## 10. Groups Panel Component

The groups panel displays group standings and matches for tournaments using `GroupsAndBracket` or `GroupsOnly` formats.

### 10.1 Component Structure

```
groups-panel/
├── groups-panel.component.ts
├── groups-panel.component.html
├── groups-panel.component.css
└── components/
    ├── group-card/
    │   ├── group-card.component.ts
    │   ├── group-card.component.html
    │   └── group-card.component.css
    └── group-match-card/
        ├── group-match-card.component.ts
        ├── group-match-card.component.html
        └── group-match-card.component.css
```

### 10.2 Groups Panel Container

**groups-panel.component.ts**
```typescript
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Standing } from '../../../core/models/standing.model';
import { Match } from '../../../core/models/match.model';
import { TournamentStatus } from '../../../core/models/tournament.model';

export interface MatchResultEvent {
  matchId: number;
  team1Score: number;
  team2Score: number;
}

@Component({
  selector: 'app-groups-panel',
  templateUrl: './groups-panel.component.html',
  styleUrls: ['./groups-panel.component.css']
})
export class GroupsPanelComponent {
  @Input() groups: Standing[] = [];
  @Input() matches: Match[] = [];
  @Input() tournamentStatus: TournamentStatus = TournamentStatus.Setup;
  @Output() matchResult = new EventEmitter<MatchResultEvent>();

  getMatchesForGroup(groupId: number): Match[] {
    return this.matches.filter(m => m.standingId === groupId);
  }

  get canEditMatches(): boolean {
    return this.tournamentStatus === TournamentStatus.GroupsInProgress;
  }

  onMatchResult(event: MatchResultEvent): void {
    this.matchResult.emit(event);
  }
}
```

**groups-panel.component.html**
```html
<div class="groups-panel">
  <!-- Empty State -->
  <div class="empty-state" *ngIf="groups.length === 0">
    <i class="pi pi-users"></i>
    <p>Groups not yet created</p>
    <small>Start the tournament to generate groups</small>
  </div>

  <!-- Groups Grid -->
  <div class="groups-grid" *ngIf="groups.length > 0">
    <app-group-card 
      *ngFor="let group of groups"
      [group]="group"
      [matches]="getMatchesForGroup(group.id)"
      [canEdit]="canEditMatches"
      (matchResult)="onMatchResult($event)">
    </app-group-card>
  </div>
</div>
```

**groups-panel.component.css**
```css
.groups-panel {
  padding: var(--space-4);
  height: 100%;
  overflow-y: auto;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  color: var(--text-muted);
}

.empty-state i {
  font-size: 3rem;
  margin-bottom: var(--space-4);
}

.empty-state p {
  margin: 0 0 var(--space-2);
  font-size: 1.125rem;
  color: var(--text-secondary);
}

.empty-state small {
  color: var(--text-muted);
}

/* Responsive grid: 1 col on small, 2 cols on wide screens */
.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: var(--space-4);
}

/* For 3+ groups, ensure max 2 columns */
@media (min-width: 1200px) {
  .groups-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
```

### 10.3 Group Card Component

**group-card.component.html**
```html
<div class="group-card">
  <!-- Header -->
  <div class="group-header">
    <h3>{{group.name}}</h3>
    <app-status-badge [status]="groupStatus"></app-status-badge>
  </div>

  <!-- Standings Table -->
  <div class="standings-table-wrapper">
    <table class="standings-table">
      <thead>
        <tr>
          <th class="col-rank">#</th>
          <th class="col-team">Team</th>
          <th class="col-stat">W</th>
          <th class="col-stat">L</th>
          <th class="col-stat">PTS</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let team of sortedTeams; let i = index"
            [class.advancing]="isAdvancing(i)"
            [class.eliminated]="isEliminated(i)">
          <td class="col-rank">{{i + 1}}</td>
          <td class="col-team">
            <span class="team-seed">{{team.seed}}</span>
            <span class="team-name">{{team.name}}</span>
          </td>
          <td class="col-stat">{{team.wins}}</td>
          <td class="col-stat">{{team.losses}}</td>
          <td class="col-stat points">{{team.points}}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Matches Section -->
  <div class="matches-section" *ngIf="matches.length > 0">
    <h4>Matches</h4>
    <div class="matches-list">
      <app-group-match-card
        *ngFor="let match of matches"
        [match]="match"
        [canEdit]="canEdit"
        (submitResult)="onSubmitResult($event)">
      </app-group-match-card>
    </div>
  </div>
</div>
```

**group-card.component.css**
```css
.group-card {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
  overflow: hidden;
}

/* Header */
.group-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-3) var(--space-4);
  background: var(--surface-overlay);
  border-bottom: 1px solid var(--surface-border);
}

.group-header h3 {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
}

/* Standings Table */
.standings-table-wrapper {
  padding: var(--space-3);
}

.standings-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.standings-table th {
  padding: var(--space-2);
  text-align: left;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  font-size: 0.75rem;
  letter-spacing: 0.05em;
  border-bottom: 1px solid var(--surface-border);
}

.standings-table td {
  padding: var(--space-2);
  color: var(--text-primary);
  border-bottom: 1px solid var(--surface-border);
}

.standings-table tbody tr:last-child td {
  border-bottom: none;
}

/* Column widths */
.col-rank { width: 40px; text-align: center; }
.col-team { /* flex */ }
.col-stat { width: 50px; text-align: center; font-family: var(--font-mono); }

/* Team cell */
.team-seed {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: var(--primary);
  color: var(--surface-ground);
  border-radius: 50%;
  font-size: 0.75rem;
  font-weight: 700;
  margin-right: var(--space-2);
}

.team-name {
  font-weight: 500;
}

/* Points emphasis */
.points {
  font-weight: 700;
  color: var(--primary);
}

/* Row states */
.standings-table tr.advancing {
  background: rgba(0, 217, 54, 0.05);
}

.standings-table tr.advancing .team-name {
  color: var(--primary);
}

.standings-table tr.eliminated {
  opacity: 0.5;
}

/* Matches Section */
.matches-section {
  border-top: 1px solid var(--surface-border);
  padding: var(--space-3) var(--space-4);
}

.matches-section h4 {
  margin: 0 0 var(--space-3);
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.matches-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}
```

### 10.4 Group Match Card Component

**group-match-card.component.html**
```html
<div class="match-card" [class.completed]="match.isCompleted" [class.editable]="canEdit && !match.isCompleted">
  <!-- Team A -->
  <div class="team-side team-a" [class.winner]="isTeamAWinner" [class.loser]="isTeamALoser">
    <span class="team-name">{{match.team1Name}}</span>
  </div>

  <!-- Score / Input -->
  <div class="score-section">
    <ng-container *ngIf="match.isCompleted || !canEdit">
      <!-- Display score -->
      <div class="score-display">
        <span class="score" [class.winner]="isTeamAWinner">{{match.team1Score}}</span>
        <span class="separator">-</span>
        <span class="score" [class.winner]="isTeamBWinner">{{match.team2Score}}</span>
      </div>
      <!-- Game indicators (for Best-of-X) -->
      <div class="game-indicators" *ngIf="match.games && match.games.length > 0">
        <span class="game-dot" 
              *ngFor="let game of match.games"
              [class.team-a-win]="game.winner === 1"
              [class.team-b-win]="game.winner === 2">
        </span>
      </div>
    </ng-container>

    <ng-container *ngIf="!match.isCompleted && canEdit">
      <!-- Editable score inputs -->
      <div class="score-input">
        <input type="number" 
               [(ngModel)]="team1Score" 
               min="0" 
               class="score-field" />
        <span class="separator">-</span>
        <input type="number" 
               [(ngModel)]="team2Score" 
               min="0" 
               class="score-field" />
      </div>
      <button pButton 
              icon="pi pi-check" 
              class="p-button-sm p-button-text submit-btn"
              (click)="submitScore()"
              [disabled]="!isValidScore">
      </button>
    </ng-container>
  </div>

  <!-- Team B -->
  <div class="team-side team-b" [class.winner]="isTeamBWinner" [class.loser]="isTeamBLoser">
    <span class="team-name">{{match.team2Name}}</span>
  </div>
</div>
```

**group-match-card.component.css**
```css
.match-card {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  padding: var(--space-2) var(--space-3);
  background: var(--surface-ground);
  border-radius: var(--radius-sm);
  gap: var(--space-3);
}

.match-card.editable {
  border: 1px dashed var(--surface-border);
}

.match-card.editable:hover {
  border-color: var(--primary);
}

/* Team sides */
.team-side {
  display: flex;
  align-items: center;
  padding: var(--space-2);
  border-radius: var(--radius-sm);
  transition: background 0.2s;
}

.team-a {
  justify-content: flex-start;
}

.team-b {
  justify-content: flex-end;
}

.team-name {
  font-weight: 500;
  color: var(--text-primary);
  font-size: 0.875rem;
}

/* Winner/Loser states */
.team-side.winner {
  background: rgba(0, 217, 54, 0.1);
}

.team-side.winner .team-name {
  color: var(--primary);
  font-weight: 700;
}

.team-side.loser {
  background: rgba(255, 71, 87, 0.1);
  opacity: 0.7;
}

/* Score section */
.score-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-1);
}

.score-display {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.score {
  font-family: var(--font-mono);
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-secondary);
  min-width: 20px;
  text-align: center;
}

.score.winner {
  color: var(--primary);
}

.separator {
  color: var(--text-muted);
}

/* Game indicators (Bo3, Bo5) */
.game-indicators {
  display: flex;
  gap: 4px;
}

.game-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--surface-border);
}

.game-dot.team-a-win {
  background: var(--primary);
}

.game-dot.team-b-win {
  background: var(--primary);
}

/* Score input */
.score-input {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.score-field {
  width: 40px;
  padding: var(--space-1);
  text-align: center;
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-sm);
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.875rem;
}

.score-field:focus {
  outline: none;
  border-color: var(--primary);
}

.submit-btn {
  margin-left: var(--space-2);
}
```

---

## 11. Bracket Panel Component

The bracket panel integrates the `brackets-viewer.js` library for tournament bracket visualization.

### 11.1 Library Reference

**Documentation:** https://github.com/Drarig29/brackets-viewer.js

**Key Points:**
- Library renders into a container element
- Requires data in `brackets-model` format
- Supports single/double elimination
- We apply custom CSS to match our theme

### 11.2 Component Structure

```
bracket-panel/
├── bracket-panel.component.ts
├── bracket-panel.component.html
├── bracket-panel.component.css
└── bracket-theme.css          # Custom theme overrides
```

### 11.3 Bracket Panel Component

**bracket-panel.component.ts**
```typescript
import { Component, Input, Output, EventEmitter, AfterViewInit, OnChanges, SimpleChanges, ElementRef, ViewChild } from '@angular/core';
import { BracketsViewer } from 'brackets-viewer';
import 'brackets-viewer/dist/brackets-viewer.min.css';

@Component({
  selector: 'app-bracket-panel',
  templateUrl: './bracket-panel.component.html',
  styleUrls: ['./bracket-panel.component.css', './bracket-theme.css']
})
export class BracketPanelComponent implements AfterViewInit, OnChanges {
  @ViewChild('bracketContainer') bracketContainer!: ElementRef;
  
  @Input() bracketData: any; // brackets-model format data
  @Input() canEdit: boolean = false;
  @Output() matchResult = new EventEmitter<any>();

  private viewer: BracketsViewer | null = null;

  ngAfterViewInit(): void {
    this.initBracket();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['bracketData'] && !changes['bracketData'].firstChange) {
      this.renderBracket();
    }
  }

  private initBracket(): void {
    if (!this.bracketContainer) return;
    
    this.viewer = new BracketsViewer();
    this.renderBracket();
  }

  private renderBracket(): void {
    if (!this.viewer || !this.bracketData) return;

    // Clear existing content
    this.bracketContainer.nativeElement.innerHTML = '';

    // Render bracket
    this.viewer.render(
      {
        stages: this.bracketData.stages,
        matches: this.bracketData.matches,
        matchGames: this.bracketData.matchGames,
        participants: this.bracketData.participants
      },
      {
        selector: '#bracket-container',
        participantOriginPlacement: 'before',
        separatedChildCountLabel: true,
        showSlotsOrigin: true,
        showLowerBracketSlotsOrigin: true,
        highlightParticipantOnHover: true
      }
    );

    // Add click handlers for editable matches
    if (this.canEdit) {
      this.attachMatchClickHandlers();
    }
  }

  private attachMatchClickHandlers(): void {
    const matchElements = this.bracketContainer.nativeElement.querySelectorAll('.match');
    matchElements.forEach((el: HTMLElement) => {
      el.addEventListener('click', (e) => this.onMatchClick(e, el));
    });
  }

  private onMatchClick(event: Event, element: HTMLElement): void {
    const matchId = element.getAttribute('data-match-id');
    if (matchId) {
      // Emit event for parent to handle (show dialog, etc.)
      this.matchResult.emit({ matchId: parseInt(matchId), action: 'edit' });
    }
  }
}
```

**bracket-panel.component.html**
```html
<div class="bracket-panel">
  <!-- Empty State -->
  <div class="empty-state" *ngIf="!bracketData">
    <i class="pi pi-sitemap"></i>
    <p>Bracket not yet generated</p>
    <small>Complete group stage or start bracket to view</small>
  </div>

  <!-- Bracket Viewer Container -->
  <div id="bracket-container" 
       #bracketContainer 
       class="bracket-container"
       *ngIf="bracketData">
  </div>
</div>
```

**bracket-panel.component.css**
```css
.bracket-panel {
  padding: var(--space-4);
  height: 100%;
  overflow: auto;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-8);
  color: var(--text-muted);
}

.empty-state i {
  font-size: 3rem;
  margin-bottom: var(--space-4);
}

.empty-state p {
  margin: 0 0 var(--space-2);
  font-size: 1.125rem;
  color: var(--text-secondary);
}

.bracket-container {
  min-height: 300px;
}
```

### 11.4 Bracket Theme Overrides

**bracket-theme.css**
```css
/* ===== BRACKETS-VIEWER.JS THEME OVERRIDES ===== */

/* Container */
.brackets-viewer {
  background: transparent !important;
  --primary-background: var(--surface-card);
  --secondary-background: var(--surface-ground);
  --match-background: var(--surface-card);
  --font-family: var(--font-family);
}

/* Round headers */
.brackets-viewer .round-header {
  background: var(--surface-overlay) !important;
  color: var(--text-secondary) !important;
  border-radius: var(--radius-sm) !important;
  font-weight: 600 !important;
  font-size: 0.875rem !important;
  padding: var(--space-2) var(--space-3) !important;
  margin-bottom: var(--space-3) !important;
}

/* Match containers */
.brackets-viewer .match {
  background: var(--surface-card) !important;
  border: 1px solid var(--surface-border) !important;
  border-radius: var(--radius-sm) !important;
  overflow: hidden !important;
}

.brackets-viewer .match:hover {
  border-color: var(--primary) !important;
}

/* Match header (Semi 1, Bo3, etc.) */
.brackets-viewer .match-header {
  background: var(--surface-overlay) !important;
  color: var(--text-muted) !important;
  font-size: 0.75rem !important;
  padding: var(--space-1) var(--space-2) !important;
  border-bottom: 1px solid var(--surface-border) !important;
}

/* Participant rows */
.brackets-viewer .participant {
  background: var(--surface-card) !important;
  color: var(--text-primary) !important;
  padding: var(--space-2) !important;
  border-bottom: 1px solid var(--surface-border) !important;
}

.brackets-viewer .participant:last-child {
  border-bottom: none !important;
}

/* Participant name */
.brackets-viewer .participant .name {
  font-weight: 500 !important;
  color: var(--text-primary) !important;
}

/* Participant score */
.brackets-viewer .participant .score {
  font-family: var(--font-mono) !important;
  font-weight: 700 !important;
  min-width: 24px !important;
  text-align: center !important;
}

/* Winner state */
.brackets-viewer .participant.win {
  background: rgba(0, 217, 54, 0.1) !important;
}

.brackets-viewer .participant.win .name {
  color: var(--primary) !important;
  font-weight: 700 !important;
}

.brackets-viewer .participant.win .score {
  color: var(--primary) !important;
}

/* Loser state */
.brackets-viewer .participant.loss {
  opacity: 0.6 !important;
}

.brackets-viewer .participant.loss .score {
  color: var(--danger) !important;
}

/* Connector lines */
.brackets-viewer .connector {
  border-color: var(--surface-border) !important;
}

/* Highlighted participant (hover) */
.brackets-viewer .participant.highlight {
  background: var(--surface-hover) !important;
}

/* BYE slots */
.brackets-viewer .participant.bye {
  opacity: 0.4 !important;
  font-style: italic !important;
}

/* Final match emphasis */
.brackets-viewer .round:last-child .match {
  border-color: var(--primary) !important;
  box-shadow: 0 0 10px rgba(0, 217, 54, 0.2) !important;
}
```

---

## 12. Results Display Component

Results are shown in the **Overview tab** (for finished tournaments) or a dedicated **Results tab**.

### 12.1 Integration Options

**Option A: Results in Overview Tab (Recommended)**
- Show results card in left info panel when tournament is `Finished`
- Already partially implemented in tournament-detail component

**Option B: Dedicated Results Tab**
- Fourth tab after Groups/Bracket
- Only visible when tournament status is `Finished`

### 12.2 Results Card Component (For Info Panel)

**results-card.component.html**
```html
<div class="results-card">
  <h3>Final Results</h3>
  
  <div class="results-list">
    <!-- Champion (1st Place) -->
    <div class="result-item champion" *ngIf="results[0]">
      <div class="place-badge gold">
        <i class="pi pi-trophy"></i>
      </div>
      <div class="result-info">
        <span class="place-label">Champion</span>
        <div class="team-row">
          <span class="team-seed">{{results[0].seed}}</span>
          <span class="team-name">{{results[0].teamName}}</span>
        </div>
      </div>
    </div>

    <!-- 2nd Place -->
    <div class="result-item" *ngIf="results[1]">
      <div class="place-badge silver">2</div>
      <div class="result-info">
        <span class="place-label">2nd Place</span>
        <div class="team-row">
          <span class="team-seed">{{results[1].seed}}</span>
          <span class="team-name">{{results[1].teamName}}</span>
        </div>
        <span class="elimination-note">{{results[1].eliminationRound}}</span>
      </div>
    </div>

    <!-- 3rd-4th Place -->
    <div class="result-item tied" *ngIf="results[2]">
      <div class="place-badge bronze">3</div>
      <div class="result-info">
        <span class="place-label">3rd-4th Place</span>
        <div class="tied-teams">
          <div class="team-row" *ngFor="let team of getThirdPlaceTeams()">
            <span class="team-seed">{{team.seed}}</span>
            <span class="team-name">{{team.teamName}}</span>
            <span class="elimination-note">{{team.eliminationRound}}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Remaining placements (5th+) -->
    <div class="result-item other" *ngFor="let result of getRemainingResults()">
      <div class="place-badge">{{result.position}}</div>
      <div class="result-info">
        <div class="team-row">
          <span class="team-seed">{{result.seed}}</span>
          <span class="team-name">{{result.teamName}}</span>
        </div>
        <span class="elimination-note">{{result.eliminationRound}}</span>
      </div>
    </div>
  </div>
</div>
```

**results-card.component.css**
```css
.results-card {
  background: var(--surface-card);
  border: 1px solid var(--surface-border);
  border-radius: var(--radius-md);
  padding: var(--space-4);
}

.results-card h3 {
  margin: 0 0 var(--space-4);
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.results-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

/* Result item */
.result-item {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-3);
  background: var(--surface-ground);
  border-radius: var(--radius-sm);
  border-left: 3px solid var(--surface-border);
}

.result-item.champion {
  border-left-color: #ffd700;
  background: rgba(255, 215, 0, 0.05);
}

/* Place badges */
.place-badge {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 0.875rem;
  flex-shrink: 0;
  background: var(--surface-border);
  color: var(--text-secondary);
}

.place-badge.gold {
  background: linear-gradient(135deg, #ffd700 0%, #ffb800 100%);
  color: #000;
  font-size: 1rem;
}

.place-badge.silver {
  background: linear-gradient(135deg, #e0e0e0 0%, #a0a0a0 100%);
  color: #000;
}

.place-badge.bronze {
  background: linear-gradient(135deg, #cd7f32 0%, #a05a20 100%);
  color: #fff;
}

/* Result info */
.result-info {
  flex: 1;
  min-width: 0;
}

.place-label {
  display: block;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.03em;
  margin-bottom: var(--space-1);
}

.team-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.team-seed {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  background: var(--primary);
  color: var(--surface-ground);
  border-radius: 50%;
  font-size: 0.7rem;
  font-weight: 700;
}

.team-name {
  font-weight: 600;
  color: var(--text-primary);
}

.elimination-note {
  display: block;
  font-size: 0.75rem;
  color: var(--text-muted);
  margin-top: 2px;
}

/* Tied teams (3rd-4th) */
.tied-teams {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.tied-teams .team-row {
  flex-wrap: wrap;
}

.tied-teams .elimination-note {
  width: 100%;
  margin-left: 30px; /* Align with team name */
}

/* Champion emphasis */
.result-item.champion .team-name {
  color: #ffd700;
  font-size: 1rem;
}
```

### 12.3 Full Results Tab (Alternative)

**results-tab.component.html**
```html
<div class="results-tab">
  <div class="results-header">
    <h2>Final Results</h2>
    <p>Tournament completed</p>
  </div>

  <div class="podium-section">
    <!-- Podium visualization -->
    <div class="podium">
      <!-- 2nd Place -->
      <div class="podium-place second">
        <div class="podium-team">
          <span class="team-seed">{{results[1]?.seed}}</span>
          <span class="team-name">{{results[1]?.teamName}}</span>
        </div>
        <div class="podium-block">
          <span class="place-number">2</span>
        </div>
      </div>

      <!-- 1st Place -->
      <div class="podium-place first">
        <div class="podium-team">
          <i class="pi pi-trophy trophy-icon"></i>
          <span class="team-seed">{{results[0]?.seed}}</span>
          <span class="team-name">{{results[0]?.teamName}}</span>
        </div>
        <div class="podium-block">
          <span class="place-number">1</span>
        </div>
      </div>

      <!-- 3rd Place -->
      <div class="podium-place third">
        <div class="podium-team">
          <span class="team-seed">{{results[2]?.seed}}</span>
          <span class="team-name">{{results[2]?.teamName}}</span>
        </div>
        <div class="podium-block">
          <span class="place-number">3</span>
        </div>
      </div>
    </div>
  </div>

  <!-- Full standings table -->
  <div class="full-standings">
    <h3>Complete Standings</h3>
    <p-table [value]="results" styleClass="p-datatable-sm">
      <ng-template pTemplate="header">
        <tr>
          <th style="width: 60px">Place</th>
          <th>Team</th>
          <th style="width: 180px">Eliminated</th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-r>
        <tr>
          <td>
            <span class="place-badge-small" [ngClass]="getPlaceClass(r.position)">
              {{r.position}}
            </span>
          </td>
          <td>
            <div class="team-cell">
              <span class="team-seed">{{r.seed}}</span>
              <span class="team-name">{{r.teamName}}</span>
            </div>
          </td>
          <td>
            <span class="elimination-text">{{r.eliminationRound || 'Champion'}}</span>
          </td>
        </tr>
      </ng-template>
    </p-table>
  </div>
</div>
```

---

## 13. Updated Tournament Detail Component

Integrating all panels into the tournament detail view.

### 13.1 Updated Tab Logic

**tournament-detail.component.ts** (additions)
```typescript
// Tab visibility based on format and status
get showGroupsTab(): boolean {
  if (!this.tournament) return false;
  return this.tournament.format === Format.GroupsAndBracket ||
         this.tournament.format === Format.GroupsOnly;
}

get showBracketTab(): boolean {
  if (!this.tournament) return false;
  return this.tournament.format === Format.BracketOnly ||
         this.tournament.format === Format.GroupsAndBracket;
}

get showResultsTab(): boolean {
  if (!this.tournament) return false;
  return this.tournament.status === TournamentStatus.Finished;
}

// Determine initial active tab
ngOnInit(): void {
  // ... existing init code ...
  
  // Set initial tab based on tournament state
  this.setInitialTab();
}

private setInitialTab(): void {
  if (!this.tournament) return;

  const status = this.tournament.status;
  const format = this.tournament.format;

  if (status === TournamentStatus.Finished) {
    // Show results tab for finished tournaments
    this.activeTabIndex = this.getResultsTabIndex();
  } else if (status === TournamentStatus.BracketInProgress) {
    // Show bracket tab during bracket stage
    this.activeTabIndex = this.getBracketTabIndex();
  } else if ([TournamentStatus.GroupsInProgress, TournamentStatus.GroupsCompleted].includes(status)) {
    // Show groups tab during group stage
    this.activeTabIndex = 0;
  } else {
    // Default to first available tab
    this.activeTabIndex = 0;
  }
}

private getBracketTabIndex(): number {
  return this.showGroupsTab ? 1 : 0;
}

private getResultsTabIndex(): number {
  let index = 0;
  if (this.showGroupsTab) index++;
  if (this.showBracketTab) index++;
  return index;
}
```

### 13.2 Updated Template

**tournament-detail.component.html** (stage panel section)
```html
<!-- Center: Stage View -->
<section class="stage-panel">
  <p-tabView [(activeIndex)]="activeTabIndex">
    
    <!-- Groups Tab -->
    <p-tabPanel header="Groups" *ngIf="showGroupsTab">
      <app-groups-panel 
        [groups]="groups"
        [matches]="groupMatches"
        [tournamentStatus]="tournament.status"
        (matchResult)="onMatchResult($event)">
      </app-groups-panel>
    </p-tabPanel>
    
    <!-- Bracket Tab -->
    <p-tabPanel header="Bracket" *ngIf="showBracketTab">
      <app-bracket-panel
        [bracketData]="bracketData"
        [canEdit]="canEditBracket"
        (matchResult)="onBracketMatchResult($event)">
      </app-bracket-panel>
    </p-tabPanel>

    <!-- Results Tab -->
    <p-tabPanel header="Results" *ngIf="showResultsTab">
      <app-results-tab
        [results]="finalResults">
      </app-results-tab>
    </p-tabPanel>

  </p-tabView>
</section>
```

---

## 14. Data Transformation Service

Service to transform backend data to `brackets-viewer.js` format.

### 14.1 Bracket Data Transformer

**bracket-transformer.service.ts**
```typescript
import { Injectable } from '@angular/core';

export interface BracketViewerData {
  stages: any[];
  matches: any[];
  matchGames: any[];
  participants: any[];
}

@Injectable({
  providedIn: 'root'
})
export class BracketTransformerService {

  /**
   * Transform backend bracket data to brackets-viewer.js format
   */
  transform(
    tournamentId: number,
    teams: any[],
    matches: any[],
    bracketStanding: any
  ): BracketViewerData {
    
    // Build participants array
    const participants = teams.map((team, index) => ({
      id: team.id,
      name: team.name,
      tournament_id: tournamentId
    }));

    // Build stage
    const stage = {
      id: bracketStanding.id,
      name: 'Main Bracket',
      type: 'single_elimination', // or 'double_elimination'
      tournament_id: tournamentId,
      number: 1,
      settings: {
        size: teams.length,
        seedOrdering: ['natural']
      }
    };

    // Build matches array
    const transformedMatches = matches.map(match => ({
      id: match.id,
      stage_id: bracketStanding.id,
      group_id: null,
      round_id: match.roundNumber,
      number: match.matchNumber,
      child_count: 0,
      status: this.mapMatchStatus(match),
      opponent1: {
        id: match.team1Id,
        score: match.team1Score,
        result: this.getResult(match, 1)
      },
      opponent2: {
        id: match.team2Id,
        score: match.team2Score,
        result: this.getResult(match, 2)
      }
    }));

    // Build match games (for Bo3, Bo5)
    const matchGames: any[] = [];
    matches.forEach(match => {
      if (match.games && match.games.length > 0) {
        match.games.forEach((game: any, index: number) => {
          matchGames.push({
            id: game.id,
            number: index + 1,
            stage_id: bracketStanding.id,
            match_id: match.id,
            opponent1: { score: game.team1Score, result: game.winnerId === match.team1Id ? 'win' : 'loss' },
            opponent2: { score: game.team2Score, result: game.winnerId === match.team2Id ? 'win' : 'loss' }
          });
        });
      }
    });

    return {
      stages: [stage],
      matches: transformedMatches,
      matchGames,
      participants
    };
  }

  private mapMatchStatus(match: any): number {
    // brackets-viewer status: 0=Pending, 1=Running, 2=Completed, 3=Archived
    if (match.isCompleted) return 2;
    if (match.team1Score > 0 || match.team2Score > 0) return 1;
    return 0;
  }

  private getResult(match: any, teamNumber: 1 | 2): string | null {
    if (!match.isCompleted) return null;
    
    const team1Won = match.team1Score > match.team2Score;
    if (teamNumber === 1) {
      return team1Won ? 'win' : 'loss';
    } else {
      return team1Won ? 'loss' : 'win';
    }
  }
}
```

---

## 15. Updated Implementation Phases

### Phase 4: Tournament Detail (Day 4-7) — EXPANDED

**Day 4-5: Groups Panel**
1. Create `GroupsPanelComponent` container
2. Create `GroupCardComponent` with standings table
3. Create `GroupMatchCardComponent` with score display/input
4. Style winner/loser states
5. Wire up match result submission

**Day 5-6: Bracket Panel**
1. Create `BracketPanelComponent` with brackets-viewer.js integration
2. Create `bracket-theme.css` with dark theme overrides
3. Create `BracketTransformerService` for data mapping
4. Test with sample bracket data
5. Add match click handlers for editing

**Day 6-7: Results & Integration**
1. Create `ResultsCardComponent` for info panel
2. Create `ResultsTabComponent` (optional full tab)
3. Update `TournamentDetailComponent` with tab logic
4. Test complete tournament lifecycle
5. Verify tab switching and state persistence

### Phase 5: Create Tournament (Day 7-8)

(Unchanged from V2)

### Phase 6: Polish (Day 8-9)

1. Loading states for all panels
2. Error handling with toasts
3. Empty states with helpful messages
4. Transition animations
5. Keyboard navigation
6. Mobile responsive breakpoints (lower priority)

---

## 16. File Checklist (Updated)

```
# Theme & Styles
[ ] src/styles/theme.css
[ ] src/styles/primeng-overrides.css

# Layouts
[ ] src/app/layouts/public-layout/
[ ] src/app/layouts/app-layout/
[ ] src/app/layouts/app-layout/sidebar/

# Shared Components
[ ] src/app/shared/components/status-badge/
[ ] src/app/shared/components/format-tag/

# Features - Landing & Auth
[ ] src/app/features/landing/
[ ] src/app/features/auth/login/
[ ] src/app/features/auth/register/

# Features - Dashboard
[ ] src/app/features/dashboard/

# Features - Tournament
[ ] src/app/features/tournament/tournament-detail/
[ ] src/app/features/tournament/create-wizard/
[ ] src/app/features/tournament/groups-panel/
[ ] src/app/features/tournament/groups-panel/components/group-card/
[ ] src/app/features/tournament/groups-panel/components/group-match-card/
[ ] src/app/features/tournament/bracket-panel/
[ ] src/app/features/tournament/bracket-panel/bracket-theme.css
[ ] src/app/features/tournament/results-card/
[ ] src/app/features/tournament/results-tab/ (optional)
[ ] src/app/features/tournament/teams-panel/

# Services
[ ] src/app/core/services/bracket-transformer.service.ts

# Routing
[ ] src/app/app-routing.module.ts
```

---

## 17. Testing Checklist

### Groups Panel
- [ ] Empty state displays when no groups
- [ ] Group standings sort correctly (by points, then wins)
- [ ] Advancing teams highlighted (top N)
- [ ] Match scores display correctly
- [ ] Match input works when editable
- [ ] Winner/loser highlighting after match complete
- [ ] Game indicators show Bo3/Bo5 progress

### Bracket Panel
- [ ] Bracket renders with brackets-viewer.js
- [ ] Theme matches dark mode design
- [ ] Winner/loser states styled correctly
- [ ] Connector lines visible
- [ ] Match click emits event for editing
- [ ] Final match has emphasis styling
- [ ] Empty state when bracket not generated

### Results
- [ ] Champion displays with trophy icon
- [ ] Podium places have correct medal colors
- [ ] Elimination round info shows
- [ ] 3rd-4th tied teams display together
- [ ] Results only visible when tournament finished

### Integration
- [ ] Tab visibility matches tournament format
- [ ] Tab auto-selects based on tournament status
- [ ] Data refreshes after match result
- [ ] Loading states during API calls
- [ ] Error toasts on failures