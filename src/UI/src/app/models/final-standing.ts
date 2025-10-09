export interface FinalStanding {
  teamId: number;
  teamName: string;
  placement: number;
  status: number;  // 0=SignedUp, 1=Competing, 2=Advanced, 3=Eliminated, 4=Champion
  eliminatedInRound: number;
}
