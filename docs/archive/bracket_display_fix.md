 FOUND THE PROBLEM! Look at this:

  0: DIV.brackets-viewer - display: none ← HIDDEN HERE
  1: DIV.bracket-container - display: block

  The .brackets-viewer element itself has display: none! All the parents are visible (Material Tab is active), but YOUR element is hidden. This must be from the component's CSS or some Angular condition.

● Read(G:\Code\TO2\src\UI\src\app\components\standing\bracket\bracket.component.css)
  ⎿  Read 66 lines

● Read(G:\Code\TO2\src\UI\src\app\components\standing\bracket\bracket.component.html)
  ⎿  Read 21 lines

● The CSS doesn't set display: none and there's no *ngIf on the element. The display: none must be coming from the brackets-viewer library itself or being set dynamically!

  Let me force the element to be visible:

● Update(G:\Code\TO2\src\UI\src\app\components\standing\bracket\bracket.component.css)
  ⎿  Updated ..\UI\src\app\components\standing\bracket\bracket.component.css with 2 additions
       26      border-radius: 8px;
       27      overflow-x: auto;
       28      min-height: 400px;
       29 +    /* CRITICAL: Force display to block - the library might be setting display:none */
       30 +    display: block !important;
       31    }
       32
       33    /* Empty state */

● Perfect! I added display: block !important to force the element to be visible. Now reload your Angular app and when you click on the Bracket tab, it should detect visibility immediately and render!