using Application.DTOs.Standing;
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
            CreateMap<Tournament, GetTournamentListResponseDTO>();
            CreateMap<CreateTournamentRequestDTO, Tournament>();
            CreateMap<Tournament, CreateTournamentResponseDTO>();
            CreateMap<Tournament, GetTournamentResponseDTO>();
            CreateMap<Tournament, GetTournamentListResponseDTO>();
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

            // Standing DTOs - Game and Match mappings
            CreateMap<Game, StandingGameDTO>();

            CreateMap<Match, BracketMatchDTO>()
                .ForMember(dest => dest.TeamAWins, opt => opt.MapFrom(src =>
                    src.Games.Count(g => g.WinnerId == src.TeamAId)))
                .ForMember(dest => dest.TeamBWins, opt => opt.MapFrom(src =>
                    src.Games.Count(g => g.WinnerId == src.TeamBId)))
                .ForMember(dest => dest.BestOf, opt => opt.MapFrom(src => (int)src.BestOf));

            CreateMap<Match, GroupMatchDTO>()
                .ForMember(dest => dest.TeamAWins, opt => opt.MapFrom(src =>
                    src.Games.Count(g => g.WinnerId == src.TeamAId)))
                .ForMember(dest => dest.TeamBWins, opt => opt.MapFrom(src =>
                    src.Games.Count(g => g.WinnerId == src.TeamBId)))
                .ForMember(dest => dest.BestOf, opt => opt.MapFrom(src => (int)src.BestOf));
        }
    }
}
