using Application.Services;
using Application.Utilities;
using CsCheck;
using Domain.Enums;

namespace Tests.Integration.Property
{
    /// <summary>
    /// Random tournaments played to the end through the real pipelines (see TournamentRunner), invariants checked on every run.
    /// A failure prints the smallest failing case CsCheck found and its seed.
    /// Replay: CsCheck_Seed=&lt;seed&gt; dotnet test --filter TournamentPropertyTests. Longer run: CsCheck_Iter=500.
    /// </summary>
    public class TournamentPropertyTests
    {
        private static readonly FormatService Formats = new();

        // The app's limits (FormatMetadata via FormatService, CreateTournamentValidator, StartRules), shared by the generator and the edge cases
        private static int MaxGroups(Format format, int maxTeams) => maxTeams / Formats.GetFormatMetadata(format).MinTeamsPerGroup!.Value;
        // validator: one group needs 2 advancing (a bracket needs 2 teams); at least one team per group is eliminated
        private static int MinAdvancing(int groups) => groups == 1 ? 2 : 1;
        private static int MaxAdvancing(int maxTeams, int groups) => maxTeams / groups - 1;

        // Every bound comes from the app's rules, so every case can be created and started and the test follows rule changes
        private static readonly Gen<TournamentCase> Cases =
            from format in Gen.OneOfConst(Format.BracketOnly, Format.GroupsOnly, Format.GroupsAndBracket)
            let meta = Formats.GetFormatMetadata(format)
            from maxTeams in Gen.Int[meta.MinTeams, meta.MaxTeams]
            from groups in meta.RequiresGroups
                ? Gen.Int[1, MaxGroups(format, maxTeams)].Select(g => (int?)g)
                : Gen.Const((int?)null)
            from advancing in format == Format.GroupsAndBracket
                ? Gen.Int[MinAdvancing(groups!.Value), MaxAdvancing(maxTeams, groups!.Value)].Select(a => (int?)a)
                : Gen.Const((int?)null)
            from teams in Gen.Int[StartRules.MinTeamsToStart(groups ?? 0), maxTeams]
            from drawSeed in Gen.Int
            from outcomes in Gen.Int[0, 3].Array[1, 64]
            select new TournamentCase(format, maxTeams, teams, groups, advancing, drawSeed, outcomes);

        [Fact]
        public async Task RandomTournaments_PlayToTheEnd_AndKeepInvariants()
        {
            await Cases.SampleAsync(async c => CheckInvariants(await TournamentRunner.PlayAsync(c)),
                iter: long.TryParse(Environment.GetEnvironmentVariable("CsCheck_Iter"), out var iter) ? iter : 50,
                print: c => c.ToString());
        }

        private static readonly int[] Mixed = [0, 3, 1, 2]; // A 2-0, B 2-1, B 2-0, A 2-1
        private static TournamentCase Case(Format f, int maxTeams, int teams, int? groups = null, int? advancing = null) =>
            new(f, maxTeams, teams, groups, advancing, DrawSeed: 1, Mixed);

        /// <summary>The corners of the app's limits, which random cases almost never hit exactly.</summary>
        public static TheoryData<string, TournamentCase> EdgeCases()
        {
            var data = new TheoryData<string, TournamentCase>();

            var bracket = Formats.GetFormatMetadata(Format.BracketOnly);
            data.Add("bracket: fewest teams to start", Case(Format.BracketOnly, bracket.MinTeams, StartRules.MinTeamsToStart(0)));
            data.Add("bracket: full", Case(Format.BracketOnly, bracket.MaxTeams, bracket.MaxTeams));

            var groupsOnly = Formats.GetFormatMetadata(Format.GroupsOnly);
            int max = groupsOnly.MaxTeams, most = MaxGroups(Format.GroupsOnly, max);
            data.Add("groups: fewest teams to start", Case(Format.GroupsOnly, groupsOnly.MinTeams, StartRules.MinTeamsToStart(1), 1));
            data.Add("groups: most groups, fewest teams", Case(Format.GroupsOnly, max, StartRules.MinTeamsToStart(most), most));
            data.Add("groups: most groups, full", Case(Format.GroupsOnly, max, max, most));

            var both = Formats.GetFormatMetadata(Format.GroupsAndBracket);
            max = both.MaxTeams;
            most = MaxGroups(Format.GroupsAndBracket, max);
            data.Add("groups+bracket: fewest teams to start", Case(Format.GroupsAndBracket, both.MinTeams, StartRules.MinTeamsToStart(1), 1, MinAdvancing(1)));
            data.Add("groups+bracket: most groups, fewest teams", Case(Format.GroupsAndBracket, max, StartRules.MinTeamsToStart(most), most, MinAdvancing(most)));
            data.Add("groups+bracket: most groups, full, fewest advance", Case(Format.GroupsAndBracket, max, max, most, MinAdvancing(most)));
            data.Add("groups+bracket: most groups, full, most advance", Case(Format.GroupsAndBracket, max, max, most, MaxAdvancing(max, most)));
            return data;
        }

        /// <summary>One group with every team: a full round robin (32 teams = 496 matches, ~30 s each). Skipped on PRs: dotnet test --filter "Category!=Slow".</summary>
        public static TheoryData<string, TournamentCase> SlowEdgeCases()
        {
            var data = new TheoryData<string, TournamentCase>();
            int groupsOnlyMax = Formats.GetFormatMetadata(Format.GroupsOnly).MaxTeams;
            int max = Formats.GetFormatMetadata(Format.GroupsAndBracket).MaxTeams;
            data.Add("groups: one full group", Case(Format.GroupsOnly, groupsOnlyMax, groupsOnlyMax, 1));
            data.Add("groups+bracket: one full group, fewest advance", Case(Format.GroupsAndBracket, max, max, 1, MinAdvancing(1)));
            data.Add("groups+bracket: one full group, most advance", Case(Format.GroupsAndBracket, max, max, 1, MaxAdvancing(max, 1)));
            return data;
        }

        [Theory]
        [MemberData(nameof(EdgeCases))]
        public async Task EdgeCaseTournaments_PlayToTheEnd_AndKeepInvariants(string name, TournamentCase c)
        {
            _ = name; // shown in the test name
            CheckInvariants(await TournamentRunner.PlayAsync(c));
        }

        [Theory]
        [Trait("Category", "Slow")]
        [MemberData(nameof(SlowEdgeCases))]
        public async Task SlowEdgeCaseTournaments_PlayToTheEnd_AndKeepInvariants(string name, TournamentCase c)
        {
            _ = name; // shown in the test name
            CheckInvariants(await TournamentRunner.PlayAsync(c));
        }

        private static void CheckInvariants(FinishedRun run)
        {
            EveryTeamHasAPlacement(run);
            PlacementsFollowSharedRankRules(run);
            NoAvoidableSameGroupPairInRoundOne(run);
        }

        private static void EveryTeamHasAPlacement(FinishedRun run)
        {
            var missing = run.Teams.Where(r => r.FinalPlacement == null).Select(r => r.Id).ToList();
            if (missing.Count > 0)
                throw new Exception($"Finished without a placement for teams {string.Join(", ", missing)}");
        }

        /// <summary>
        /// Bracket team: 1 + number of teams that got further (1, 2, 3, 3, 5...).
        /// GroupsAndBracket, out in the groups: below all bracket teams, same group position = same rank.
        /// GroupsOnly: the group position.
        /// </summary>
        private static void PlacementsFollowSharedRankRules(FinishedRun run)
        {
            var groupPosition = new Dictionary<long, int>();
            foreach (var group in run.Standings.Where(s => s.StandingType == StandingType.Group))
            {
                var ranked = GroupRanking.Rank(run.Entries.Where(e => e.StandingId == group.Id), run.Matches.Where(m => m.StandingId == group.Id));
                for (int i = 0; i < ranked.Count; i++)
                    groupPosition[ranked[i].TeamId] = i + 1;
            }

            // How far a bracket team got: the round it lost, the champion never lost
            var bracket = run.Standings.SingleOrDefault(s => s.StandingType == StandingType.Bracket);
            var reached = new Dictionary<long, int>();
            if (bracket != null)
            {
                foreach (var m in run.Matches.Where(m => m.StandingId == bracket.Id))
                    foreach (var team in new[] { m.TeamAId ?? 0, m.TeamBId ?? 0 }.Where(t => t > 0))
                        reached[team] = team == m.WinnerId ? reached.GetValueOrDefault(team, int.MaxValue) : m.Round!.Value;
            }

            var expected = new Dictionary<long, int>();
            foreach (var r in run.Teams)
            {
                if (run.Tournament.Format == Format.GroupsOnly)
                    expected[r.Id] = groupPosition[r.Id];
                else if (reached.TryGetValue(r.Id, out var round))
                    expected[r.Id] = 1 + reached.Values.Count(other => other > round);
                else
                    expected[r.Id] = 1 + reached.Count + groupPosition.Count(g => !reached.ContainsKey(g.Key) && g.Value < groupPosition[r.Id]);
            }

            var wrong = run.Teams.Where(r => r.FinalPlacement != expected[r.Id])
                .Select(r => $"team {r.Id}: {r.FinalPlacement}, expected {expected[r.Id]}").ToList();
            if (wrong.Count > 0)
                throw new Exception($"Placements break the shared-rank rules: {string.Join("; ", wrong)}");
        }

        /// <summary>
        /// Teams playing real round 1 matches (BYEs left out) can all avoid a same-group opponent
        /// unless one group holds more than half of them. So a same-group pair is only allowed then.
        /// </summary>
        private static void NoAvoidableSameGroupPairInRoundOne(FinishedRun run)
        {
            if (run.Tournament.Format != Format.GroupsAndBracket)
                return;

            var groupOf = run.Entries.ToDictionary(e => e.TeamId, e => e.StandingId);
            var bracketId = run.Standings.Single(s => s.StandingType == StandingType.Bracket).Id;
            var roundOne = run.Matches
                .Where(m => m.StandingId == bracketId && m.Round == 1 && m.TeamAId > 0 && m.TeamBId > 0)
                .Select(m => (A: m.TeamAId!.Value, B: m.TeamBId!.Value))
                .ToList();

            var clashes = roundOne.Where(p => groupOf[p.A] == groupOf[p.B]).ToList();
            if (clashes.Count == 0)
                return;

            var playing = roundOne.SelectMany(p => new[] { p.A, p.B }).ToList();
            int largestGroup = playing.GroupBy(t => groupOf[t]).Max(g => g.Count());
            if (largestGroup * 2 <= playing.Count)
                throw new Exception($"Avoidable same-group round 1 pairs: {string.Join(", ", clashes.Select(p => $"{p.A} vs {p.B}"))} " +
                                    $"(largest group has {largestGroup} of {playing.Count} round 1 players)");
        }
    }
}
