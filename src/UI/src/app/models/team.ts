export interface Team {
  id: number;
  name: string;
  wins: number;
  losses: number;
  points: number;
  status?: number; // 0=SignedUp, 1=Competing, 2=Advanced, 3=Eliminated, 4=Champion
}
