import { Format } from '../models/tournament';

/**
 * Single source of truth for tournament format configuration on the frontend.
 * Provides display names and format-related helper methods.
 * Mirrors the backend ITournamentFormatConfiguration service.
 */
export class FormatConfig {
  /**
   * Gets the display label for a tournament format.
   */
  static getFormatLabel(format: Format): string {
    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.BracketAndGroup:
        return 'Group Stage + Bracket';
      default:
        return 'Unknown Format';
    }
  }

  /**
   * Checks if the format requires group stage.
   */
  static requiresGroups(format: Format): boolean {
    return format === Format.BracketAndGroup;
  }

  /**
   * Checks if the format requires bracket stage.
   */
  static requiresBracket(format: Format): boolean {
    return true; // All current formats have brackets
  }

  /**
   * Gets all valid format values.
   */
  static getValidFormats(): Format[] {
    return [Format.BracketOnly, Format.BracketAndGroup];
  }
}
