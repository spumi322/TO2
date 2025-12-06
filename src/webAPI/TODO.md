# Landing Page
### Goal: Starting screen for the app with rich info.
### Current structure: 
1. Hero section -> Get started
2. Key Features section -> Short, compact feature display
3. How it works section -> Step by step explanation
4. Demo/Screenshot section -> Placeholder AI bullshitting

### Things to improve
- Apply design of the more progressed components like, auth, create-tournament, tournament-list, etc.
- Review blocks, content and layout-wise.
- Remove demo/screenshot section.

### UX upgrade
- Implemet polished demo/tour/screenshot section.
## Priority: 2/5


# Auth Page (Register and Login)

### Goal: Handle authentication
### Current structure: 
1. Register -> Registration form for new users
2. Login -> Sign In option for registered users

### Things to improve
- Validation Error massages and Success massage should work and look like in create-tournament component.
- Component positioning, validation error masssage expands the current layout out of screen.

### UX upgrade
- Try using chrome's password manager functions for password fields.

## Priority: 1/5


# Tournament-List

### Goal: Main Navigation Page 
### Current structure: 
1. Three section grid for tournaments, based on status

### Things to improve
- Empty State is justa a header, replace it.
- Tournament Card: Format is broken, always shows Unknown Format.
- Minor section layout fix.

### UX upgrade
- Search field for tournaments
- Jump to section
## Priority: 1/5

# Create Tournament

### Goal: Create new tournament
### Current structure: 
1. Simple form, team configuration fields changing based on selected format

### Things to improve
- Implement Groups Only format. In the backend it is supported, but not yet implemented.
- Change Description field from resizeable.
- Success message (popup toast?) should be replaced or removed completly.

### UX upgrade
- Better Tournament Format Selector, with small pictures and more explanation of the rules
## Priority: 5/5

# Navbar

### Goal: Quick access to features and functions
### Current structure: 
Sections from left to right:
1. Logo
2. Navigation
3. Tournament info
4. Tournament management functions
5. User info

### Things to improve
- Fix layout during different elements, when tournament info is empty, the layout shrinks to left. 
- Tournament management functions have different button design.
- Clicking on User Info dropdowns a Logout button, inconsistent look.

## Priority: 4/5

# Tournament Details

### Goal: Main Tournament Admin Page
### Current structure: 
Multi-phase layout based on Tournament State and tabs

### Things to improve
- General:
1. Too much space between container and navabar.
- Upcoming tournament:
1. Registered Teams do not look like teams, should be redesigned.
2. Implement duplicate team name error message.
- Ongoing tournament:
1. Overview tab missing top right element.
2. Group stage tab needs layout and design overview.
3. Bracket tab needs layout and design overview.
- Finished tournament:
1. Overview tab results element redesign
2. Results tab need overhaul

## Priority: 4/5