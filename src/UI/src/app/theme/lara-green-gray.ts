import { definePreset } from '@primeng/themes';
import Lara from '@primeng/themes/lara';

export const LaraGreenGray = definePreset(Lara, {
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
          50: '#f5f5f5',
          100: '#e0e0e0',
          200: '#bdbdbd',
          300: '#9e9e9e',
          400: '#757575',
          500: '#616161',  // Surface gray
          600: '#424242',
          700: '#303030',
          800: '#212121',
          900: '#121212',
          950: '#0a0a0a'
        }
      }
    }
  }
});
