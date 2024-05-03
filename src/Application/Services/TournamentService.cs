using Application.Contracts;
using Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IGenericRepository<Tournament> _tournamentRepository;

        public TournamentService(IGenericRepository<Tournament> tournamentRepository)
        {
            _tournamentRepository = tournamentRepository;
        }

        public async Task<Tournament> CreateTournamentAsync(CreateTournamentDTO request)
        {
            var tournament = new Tournament(request.Name, request.Description, request.MaxTeams, request.StartDate, request.EndDate, request.Format);

            try
            {
                await _tournamentRepository.Add(tournament);
                await _tournamentRepository.Save();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task GetTournamentAsync()
        {
            throw new NotImplementedException();
        }

        public Task GetAllTournamentsAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateTournamentAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteTournamentAsync()
        {
            throw new NotImplementedException();
        }
    }
}
