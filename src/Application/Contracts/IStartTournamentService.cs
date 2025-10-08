using Application.DTOs.Tournament;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface IStartTournamentService
    {
        Task<TournamentSeedResult> InitializeTournamentAsync(long tournamentId);
    }
}
