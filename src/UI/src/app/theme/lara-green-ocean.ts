import { definePreset } from '@primeng/themes';
import Lara from '@primeng/themes/lara';

export const LaraGreenOcean = definePreset(Lara, {
  semantic: {
    primary: {
      50: '#e8f5e9',
      100: '#c8e6c9',
      200: '#a5d6a7',
      300: '#81c784',
      400: '#66bb6a',
      500: '#4caf50',  // Primary green
      600: '#43a047',
      700: '#388e3c',
      800: '#2e7d32',
      900: '#1b5e20',
      950: '#0d3818'
    },
    colorScheme: {
      light: {
        surface: {
          0: '#ffffff',
          50: '#f0f9ff',    // Ocean - lightest cyan
          100: '#e0f2fe',   // Ocean - very light cyan
          200: '#bae6fd',   // Ocean - light cyan
          300: '#7dd3fc',   // Ocean - cyan
          400: '#38bdf8',   // Ocean - medium cyan
          500: '#0ea5e9',   // Ocean - cyan blue
          600: '#0284c7',   // Ocean - deep cyan
          700: '#0369a1',   // Ocean - dark cyan
          800: '#075985',   // Ocean - darker cyan
          900: '#0c4a6e',   // Ocean - very dark cyan
          950: '#082f49'    // Ocean - deepest cyan
        }
      },
      dark: {
        surface: {
          0: '#0c1821',
          50: '#082f49',    // Ocean dark - deepest
          100: '#0c4a6e',   // Ocean dark - very dark
          200: '#075985',   // Ocean dark - darker
          300: '#0369a1',   // Ocean dark - dark
          400: '#0284c7',   // Ocean dark - medium
          500: '#0ea5e9',   // Ocean dark - base
          600: '#38bdf8',   // Ocean dark - light
          700: '#7dd3fc',   // Ocean dark - lighter
          800: '#bae6fd',   // Ocean dark - very light
          900: '#e0f2fe',   // Ocean dark - lightest
          950: '#f0f9ff'    // Ocean dark - almost white
        }
      }
    }
  }
});
