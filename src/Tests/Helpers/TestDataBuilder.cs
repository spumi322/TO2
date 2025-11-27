using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;

namespace Tests.Helpers
{
    /// <summary>
    /// Fluent builder for creating test data objects.
    /// Provides default values but allows customization.
    /// </summary>
    public static class TestDataBuilder
    {
        public static class Tournaments
        {
            public static Tournament CreateDefault(
                string name = "Test Tournament",
                string description = "Test Description",
                int maxTeams = 8,
                Format format = Format.BracketOnly)
            {
                return new Tournament(name, description, maxTeams, format);
            }

            public static Tournament CreateGroupsAndBracket(
                string name = "Test Tournament with Groups",
                int maxTeams = 8)
            {
                return new Tournament(name, "Test with groups", maxTeams, Format.GroupsAndBracket);
            }

            public static Tournament CreateInStatus(
                TournamentStatus status,
                string name = "Test Tournament")
            {
                var tournament = CreateDefault(name: name);
                tournament.Status = status;
                return tournament;
            }
        }

        public static class Teams
        {
            private static int _counter = 0;

            public static Team CreateDefault(string? name = null)
            {
                _counter++;
                return new Team(name ?? $"Test Team {_counter}");
            }

            public static List<Team> CreateMultiple(int count)
            {
                var teams = new List<Team>();
                for (int i = 0; i < count; i++)
                {
                    teams.Add(CreateDefault($"Team {i + 1}"));
                }
                return teams;
            }

            public static void ResetCounter()
            {
                _counter = 0;
            }
        }

        public static class TournamentTeams
        {
            public static TournamentTeam Create(long tournamentId, long teamId)
            {
                return new TournamentTeam(tournamentId, teamId);
            }
        }

        public static class Standings
        {
            public static Standing CreateGroup(
                long tournamentId,
                string name = "Group A",
                int maxTeams = 4)
            {
                var standing = new Standing(name, maxTeams, StandingType.Group)
                {
                    TournamentId = tournamentId,
                    IsSeeded = false
                };
                return standing;
            }

            public static Standing CreateBracket(
                long tournamentId,
                string name = "Bracket",
                int maxTeams = 8)
            {
                var standing = new Standing(name, maxTeams, StandingType.Bracket)
                {
                    TournamentId = tournamentId,
                    IsSeeded = false
                };
                return standing;
            }
        }

        public static class Matches
        {
            public static Match Create(
                long standingId,
                Team teamA,
                Team teamB,
                BestOf bestOf = BestOf.Bo1)
            {
                var match = new Match(teamA, teamB, bestOf)
                {
                    StandingId = standingId
                };
                return match;
            }

            public static Match CreateWithoutTeams(
                long standingId,
                BestOf bestOf = BestOf.Bo1)
            {
                // Use parameterless constructor for cases where teams aren't available yet
                var match = new Match()
                {
                    StandingId = standingId,
                    BestOf = bestOf
                };
                return match;
            }
        }

        public static class Games
        {
            public static Game Create(
                Match match,
                long? teamAId,
                long? teamBId)
            {
                return new Game(match, teamAId, teamBId);
            }

            public static Game CreateCompleted(
                Match match,
                long? teamAId,
                long? teamBId,
                long winnerId,
                int teamAScore = 10,
                int teamBScore = 5)
            {
                var game = new Game(match, teamAId, teamBId);
                game.WinnerId = winnerId;
                game.TeamAScore = teamAScore;
                game.TeamBScore = teamBScore;
                return game;
            }
        }

        public static class Groups
        {
            public static Group Create(
                long tournamentId,
                long standingId,
                Team team)
            {
                return new Group(tournamentId, standingId, team);
            }

            public static Group CreateWithIds(
                long tournamentId,
                long standingId,
                long teamId,
                string teamName)
            {
                return new Group(tournamentId, standingId, teamId, teamName);
            }
        }
    }
}
