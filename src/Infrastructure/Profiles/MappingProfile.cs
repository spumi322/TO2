using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;

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
            CreateMap<GetTeamResponseDTO, Team>();
            CreateMap<Group, GetTeamWithStatsResponseDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.TeamName))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TeamId));
        }
    }
}
