using Application.Contracts;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Services
{
    public class FormatService : IFormatService
    {
        private readonly Dictionary<Format, FormatMetadata> _formats;

        private readonly Dictionary<BestOf, int> _bestOfToTotalGames = new()
        {
            { BestOf.Bo1, 1 },
            { BestOf.Bo3, 3 },
            { BestOf.Bo5, 5 }
        };

        private readonly Dictionary<BestOf, int> _bestOfToGamesToWin = new()
        {
            { BestOf.Bo1, 1 },
            { BestOf.Bo3, 2 },
            { BestOf.Bo5, 3 }
        };

        public FormatService()
        {
            _formats = FormatMetadata.All.ToDictionary(f => f.Format, f => f);
        }

        private const BestOf DefaultBestOf = BestOf.Bo3;
        public BestOf GetDefaultBestOf() => DefaultBestOf;

        public int GetTotalGames(BestOf bestOf)
        {
            if (!_bestOfToTotalGames.ContainsKey(bestOf))
                throw new ArgumentOutOfRangeException(nameof(bestOf), $"Invalid BestOf value: {bestOf}");

            return _bestOfToTotalGames[bestOf];
        }

        public int GetGamesToWin(BestOf bestOf)
        {
            if (!_bestOfToGamesToWin.ContainsKey(bestOf))
                throw new ArgumentOutOfRangeException(nameof(bestOf), $"Invalid BestOf value: {bestOf}");

            return _bestOfToGamesToWin[bestOf];
        }
        public FormatMetadata GetFormatMetadata(Format format)
        {
            if (!_formats.ContainsKey(format))
                throw new ArgumentOutOfRangeException(nameof(format), $"Invalid format: {format}");

            return _formats[format];
        }

        public int CalculateNumberOfGroups(Format format, int maxTeams, int teamsPerGroup)
        {
            var metadata = GetFormatMetadata(format);
            if (!metadata.RequiresGroups)
                throw new InvalidOperationException($"Format {format} does not use groups");

            return maxTeams / teamsPerGroup;
        }
    }
}
