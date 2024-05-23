using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateTournamentRequestDTO, Tournament>();
            CreateMap<Tournament, CreateTournamentResponseDTO>();
            CreateMap<Tournament, GetTournamentResponseDTO>();
            CreateMap<Tournament, GetAllTournamentsResponseDTO>();
            CreateMap<Tournament, UpdateTournamentResponseDTO>();
            CreateMap<UpdateTournamentRequestDTO, Tournament>();
            CreateMap<CreateTeamRequestDTO, Team>();
            CreateMap<Team, CreateTeamResponseDTO>();
            CreateMap<Team, GetTeamResponseDTO>();
            CreateMap<Team, UpdateTeamResponseDTO>();
            CreateMap<UpdateTeamRequestDTO, Team>();    
            CreateMap<Team, GetAllTeamsResponseDTO>();

        }
    }
}
