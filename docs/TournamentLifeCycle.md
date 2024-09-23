### Tournament LifeCycle


## 1. Create New Tournament

- Create a new tournament by clicking on the `Create New Tournament` button.

- Fields: 	
Tournament Name / must be unique
Description / optional
Max Teams / minimum 4 maximum 64
Format / Bracket Only, Bracket and Groups 
Start Date / will be added later with scheduling
End Date / will be added later with scheduling

This sets the tournament status to **Upcoming**, and **RegistrationOpen** true,**CanSetMatchScore** false, **GroupsFinished** false, **BracketFinished** false.
Teams can be added manually, or by sharing the tournament link with the team captains (later feature). 

## 2. Start Tournament

- Starting the tournament by clicking on the `Start Tournament` button. 
This will set the tournament status to **In Progress**, and **RegistrationOpen** false, **CanSetMatchScore** true, **GroupsFinished** false, **BracketFinished** false.

Seeding the groups will be done automatically by the system (random without seeding weights). Groups can be viewed by clicking on the `Groups` tab.

## 3. Playing Matches

Match scores are set by manually clicking the `+` button on the match card. After finishing all matches in the group, the group status will be set to **IsFinished** true
and the top teams will be moved to the bracket. SeedBracket() will be called to seed the bracket by pairing the top teams from each group.

## 4. End Tournament

After the winner is determined, the flags are **CanSetMatchScore** false, **IsFinished** true, TournamentStatus Finished and the end result will be displayed on the tournament page.
Prize pool will be distributed to the winners (later feature).




 tournament status

 registration open
 can set match score
 is finished
	
