using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Contracts
{
    public interface IFormatService
    {
        BestOf GetDefaultBestOf();
        int GetTotalGames(BestOf bestOf);
        int GetGamesToWin(BestOf bestOf);
        FormatMetadata GetFormatMetadata(Format format);
        int CalculateNumberOfGroups(Format format, int maxTeams, int teamsPerGroup);
    }
}
