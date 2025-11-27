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
      case Format.GroupsAndBracket:
        return 'Groups + Bracket';
      case Format.GroupsOnly:
        return 'Groups Only';
      default:
        return 'Unknown Format';
    }
  }

  /**
   * Checks if the format requires group stage.
   */
  static requiresGroups(format: Format): boolean {
    return format === Format.GroupsAndBracket || format === Format.GroupsOnly;
  }

  /**
   * Checks if the format requires bracket stage.
   */
  static requiresBracket(format: Format): boolean {
    return format === Format.BracketOnly || format === Format.GroupsAndBracket;
  }

  /**
   * Gets all valid format values.
   */
  static getValidFormats(): Format[] {
    return [Format.BracketOnly, Format.GroupsAndBracket, Format.GroupsOnly];
  }
}
