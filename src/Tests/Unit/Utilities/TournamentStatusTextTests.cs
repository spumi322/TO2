using Application.Utilities;
using Domain.Enums;
using FluentAssertions;

namespace Tests.Unit.Utilities
{
    public class TournamentStatusTextTests
    {
        [Fact]
        public void Describe_HasWordsForEveryStatus_NoEnumNames()
        {
            foreach (var status in Enum.GetValues<TournamentStatus>())
            {
                var text = TournamentStatusText.Describe(status);
                text.Should().NotBe("in another stage");
                text.Should().NotContain(status.ToString());
            }
        }
    }
}
