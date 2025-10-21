/**
 * TypeScript declarations for brackets-viewer.js global window object
 */

declare interface Window {
  bracketsViewer: {
    render(data: {
      stages: any[];
      matches: any[];
      matchGames: any[];
      participants: any[];
    }, config?: {
      onMatchClick?: (match: any) => void;
      [key: string]: any;
    }): void;
  };
}
