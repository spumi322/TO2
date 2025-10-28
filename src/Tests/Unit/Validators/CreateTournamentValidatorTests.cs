using Application.DTOs.Tournament;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Tests.Unit.Validators
{
    public class CreateTournamentValidatorTests
    {
        private readonly CreateTournamentValidator _validator;

        public CreateTournamentValidatorTests()
        {
            _validator = new CreateTournamentValidator();
        }

        // Helper to create valid DTO with defaults
        private CreateTournamentRequestDTO CreateValidDTO(
            string? name = null,
            string? description = null,
            int? maxTeams = null,
            Format? format = null,
            int? teamsPerGroup = null,
            int? teamsPerBracket = null)
        {
            return new CreateTournamentRequestDTO(
                Name: name ?? "Test Tournament",
                Description: description ?? "Test Description",
                MaxTeams: maxTeams ?? 8,
                Format: format ?? Format.BracketOnly,
                TeamsPerGroup: teamsPerGroup,
                TeamsPerBracket: teamsPerBracket ?? 8
            );
        }

        #region Name Validation

        [Fact]
        public void Should_HaveError_When_Name_IsEmpty()
        {
            var request = CreateValidDTO(name: "");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("A")]
        public void Should_HaveError_When_Name_IsTooShort(string name)
        {
            var request = CreateValidDTO(name: name);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_HaveError_When_Name_IsTooLong()
        {
            var request = CreateValidDTO(name: new string('A', 101));
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData("Test")]
        [InlineData("Valid Tournament Name")]
        public void Should_NotHaveError_When_Name_IsValid(string name)
        {
            var request = CreateValidDTO(name: name);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Description Validation

        [Fact]
        public void Should_HaveError_When_Description_ExceedsMaxLength()
        {
            var request = CreateValidDTO(description: new string('A', 251));
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        #endregion

        #region MaxTeams Validation

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        public void Should_HaveError_When_MaxTeams_IsBelowMinimum(int maxTeams)
        {
            var request = CreateValidDTO(maxTeams: maxTeams, teamsPerBracket: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams);
        }

        [Theory]
        [InlineData(33)]
        [InlineData(100)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum(int maxTeams)
        {
            var request = CreateValidDTO(maxTeams: maxTeams, teamsPerBracket: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        public void Should_NotHaveError_When_MaxTeams_IsInValidRange(int maxTeams)
        {
            var request = CreateValidDTO(maxTeams: maxTeams, teamsPerBracket: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.MaxTeams);
        }

        #endregion

        #region TeamsPerBracket Validation

        [Theory]
        [InlineData(3)]
        [InlineData(0)]
        public void Should_HaveError_When_TeamsPerBracket_IsBelowMinimum(int teamsPerBracket)
        {
            var request = CreateValidDTO(teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket);
        }

        [Theory]
        [InlineData(33)]
        [InlineData(100)]
        public void Should_HaveError_When_TeamsPerBracket_IsAboveMaximum(int teamsPerBracket)
        {
            var request = CreateValidDTO(teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket);
        }

        #endregion

        #region TeamsPerGroup Validation

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        public void Should_HaveError_When_TeamsPerGroup_IsBelowMinimum_ForBracketAndGroup(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.BracketAndGroup, maxTeams: 8, teamsPerGroup: teamsPerGroup, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup);
        }

        [Theory]
        [InlineData(17)]
        [InlineData(100)]
        public void Should_HaveError_When_TeamsPerGroup_IsAboveMaximum_ForBracketAndGroup(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.BracketAndGroup, maxTeams: 16, teamsPerGroup: teamsPerGroup, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup);
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerGroup_IsNotNull_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, teamsPerGroup: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup);
        }

        #endregion

        #region Format-Specific Rules

        [Fact]
        public void Should_HaveError_When_MaxTeams_NotEquals_TeamsPerBracket_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 16, teamsPerBracket: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Should_NotHaveError_When_MaxTeams_Equals_TeamsPerBracket_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 8, teamsPerBracket: 8, teamsPerGroup: null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Should_HaveError_When_MaxTeams_NotDivisibleBy_TeamsPerGroup_ForBracketAndGroup()
        {
            var request = CreateValidDTO(format: Format.BracketAndGroup, maxTeams: 10, teamsPerGroup: 3, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Should_NotHaveError_When_MaxTeams_DivisibleBy_TeamsPerGroup_ForBracketAndGroup()
        {
            var request = CreateValidDTO(format: Format.BracketAndGroup, maxTeams: 12, teamsPerGroup: 4, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion
    }
}
