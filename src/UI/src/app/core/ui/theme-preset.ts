import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

// The original TO2 greens (material green; #43a047 is the 600 shade)
const green = {
  50: '#e8f5e9',
  100: '#c8e6c9',
  200: '#a5d6a7',
  300: '#81c784',
  400: '#66bb6a',
  500: '#4caf50',
  600: '#43a047',
  700: '#388e3c',
  800: '#2e7d32',
  900: '#1b5e20',
  950: '#0f3d14',
};

// Aura with the TO2 green primary; dark surfaces follow the navy/slate tokens in styles.css.
// Button colors are picked for contrast: dark text on green in dark mode, white on dark green in light mode.
export const To2Preset = definePreset(Aura, {
  semantic: {
    primary: green,
    colorScheme: {
      light: {
        primary: {
          color: '{primary.800}',
          contrastColor: '#ffffff',
          hoverColor: '{primary.900}',
          activeColor: '{primary.900}',
        },
      },
      dark: {
        primary: {
          color: '{primary.500}',
          contrastColor: '#0b1120',
          hoverColor: '{primary.400}',
          activeColor: '{primary.300}',
        },
        surface: {
          0: '#ffffff',
          50: '#dce8dd',
          100: '#c4cdd6',
          200: '#9ca3af',
          300: '#7b8494',
          400: '#5b6475',
          500: '#4b5563',
          600: '#374151',
          700: '#2d3340',
          800: '#242938',
          900: '#1a1f2e',
          950: '#0b1120',
        },
      },
    },
  },
});
