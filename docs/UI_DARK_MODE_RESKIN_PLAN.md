# TO2 UI Dark Mode Reskin - Implementation Plan

## Executive Summary

Transform TO2 from Lara Light Green (ocean surfaces) to Lara Dark Green as default theme. This is a **CSS-only** reskin with NO functionality changes. Main work: swap theme file, update CSS variable system, reskin components systematically by user flow.

**Current:** Light mode with ocean cyan surfaces + green primary  
**Target:** Dark mode with dark gray surfaces + green primary  
**Scope:** 20 component CSS files, 1 global theme file, 195+ CSS variable usages  
**WIP Marking:** GroupsOnly format (no backend pipeline yet)

---

## Phase 1: Global Theme Infrastructure (Foundation)

### 1.1 Swap PrimeNG Base Theme

**File:** `G:/Code/TO2/src/UI/angular.json`

**Changes:**
```json
"styles": [
  // BEFORE:
  "node_modules/primeng/resources/themes/lara-light-green/theme.css",
  
  // AFTER:
  "node_modules/primeng/resources/themes/lara-dark-green/theme.css",
  
  "src/app/theme/ocean-surface-override.css",  // KEEP for now
  "node_modules/brackets-viewer/dist/brackets-viewer.min.css",
  "src/styles.css"
]
```

**Why:** Changes base PrimeNG components to dark mode  
**Test:** Run `npm start`, verify buttons/inputs are dark themed

---

### 1.2 Create Dark Mode CSS Variable System

**File:** `G:/Code/TO2/src/UI/src/app/theme/dark-mode-variables.css` (NEW FILE)

**Content:**
```css
/* Dark Mode Design Tokens for TO2 */
:root {
  /* ===== PRIMARY COLOR (Green - unchanged) ===== */
  --primary-color: #4caf50;
  --primary-dark: #388e3c;
  --primary-light: #66bb6a;
  
  /* ===== BACKGROUND COLORS (Dark Gray) ===== */
  --bg-dark: #0a0e1a;          /* Darkest - page background */
  --bg-card: #151922;          /* Cards, panels */
  --bg-card-hover: #1c212b;   /* Hover state */
  
  /* ===== SURFACE COLORS (Dark Gray Scale) ===== */
  --surface-0: #0a0e1a;
  --surface-50: #0f1419;
  --surface-100: #151922;
  --surface-200: #1c212b;
  --surface-300: #252b38;
  --surface-400: #2e3644;
  --surface-500: #3a4254;
  --surface-600: #4a5468;
  --surface-700: #5c6780;
  --surface-800: #707c96;
  --surface-900: #8891a8;
  --surface-950: #a0a9be;
  
  /* ===== TEXT COLORS ===== */
  --text-primary: #e8eaf0;     /* Main text */
  --text-secondary: #b8bcc8;   /* Secondary text */
  --text-muted: #8891a8;       /* Muted/disabled text */
  --text-dark: #0a0e1a;        /* Text on green buttons */
  
  /* ===== BORDER COLORS ===== */
  --border-color: #252b38;
  --border-width: 1px;
  --border-radius: 6px;
  
  /* ===== SEMANTIC COLORS ===== */
  --success-color: #00d936;
  --error-color: #ff4444;
  --warning-color: #ffaa00;
  --info-color: #3b82f6;
  
  /* ===== SHADOWS & GLOWS ===== */
  --shadow-card: 0 2px 8px rgba(0, 0, 0, 0.3);
  --shadow-elevated: 0 4px 16px rgba(0, 0, 0, 0.4);
  --glow-primary: 0 0 20px rgba(76, 175, 80, 0.4);
  
  /* ===== PRIMENG VARIABLE OVERRIDES ===== */
  --p-primary-color: var(--primary-color);
  --p-surface-0: var(--surface-0);
  --p-surface-50: var(--surface-50);
  --p-surface-100: var(--surface-100);
  --p-surface-200: var(--surface-200);
  --p-surface-300: var(--surface-300);
  --p-surface-card: var(--bg-card);
  --p-text-color: var(--text-primary);
  --p-text-secondary-color: var(--text-muted);
}
```

**Why:** Central token system for dark mode  
**Test:** None yet (just file creation)

---
