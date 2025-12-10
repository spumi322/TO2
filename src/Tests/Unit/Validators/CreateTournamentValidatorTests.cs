using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Services;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Tests.Unit.Validators
{
    public class CreateTournamentValidatorTests
    {
        private readonly CreateTournamentValidator _validator;

        public CreateTournamentValidatorTests()
        {
            IFormatService formatService = new FormatService();
            _validator = new CreateTournamentValidator(formatService);
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
                MaxTeams: maxTeams ?? 16,
                Format: format ?? Format.GroupsAndBracket,
                TeamsPerGroup: teamsPerGroup,
                TeamsPerBracket: teamsPerBracket
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
        [InlineData(3)]
        public void Should_HaveError_When_MaxTeams_IsBelowMinimum_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams, teamsPerBracket: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        [InlineData(64)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams, teamsPerBracket: 32);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        public void Should_NotHaveError_When_MaxTeams_IsInValidRange_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams, teamsPerBracket: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.MaxTeams);
        }

        [Theory]
        [InlineData(3)]
        public void Should_HaveError_When_MaxTeams_IsBelowMinimum_ForGroupsAndBracket(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: maxTeams, teamsPerGroup: 4, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum_ForGroupsAndBracket(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: maxTeams, teamsPerGroup: 4, teamsPerBracket: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(3)]
        public void Should_HaveError_When_MaxTeams_IsBelowMinimum_ForGroupsOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: maxTeams, teamsPerGroup: 4, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum_ForGroupsOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: maxTeams, teamsPerGroup: 4, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        #endregion

        #region TeamsPerBracket Validation

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void Should_HaveError_When_TeamsPerBracket_IsBelowMinimum_ForBracketOnly(int teamsPerBracket)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: teamsPerBracket, teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        [InlineData(64)]
        public void Should_HaveError_When_TeamsPerBracket_IsAboveMaximum_ForBracketOnly(int teamsPerBracket)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 32, teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket must be between 4 and 32.");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        public void Should_NotHaveError_When_TeamsPerBracket_IsInValidRange_ForBracketOnly(int teamsPerBracket)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: teamsPerBracket, teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.TeamsPerBracket);
        }

        [Theory]
        [InlineData(3)]
        public void Should_HaveError_When_TeamsPerBracket_IsBelowMinimum_ForGroupsAndBracket(int teamsPerBracket)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 8, teamsPerGroup: 4, teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        public void Should_HaveError_When_TeamsPerBracket_IsAboveMaximum_ForGroupsAndBracket(int teamsPerBracket)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 8, teamsPerGroup: 4, teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket must be between 4 and 32.");
        }

        #endregion

        #region TeamsPerGroup Validation

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void Should_HaveError_When_TeamsPerGroup_IsBelowMinimum_ForGroupsAndBracket(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 32, teamsPerGroup: teamsPerGroup, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        [InlineData(64)]
        public void Should_HaveError_When_TeamsPerGroup_IsAboveMaximum_ForGroupsAndBracket(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 32, teamsPerGroup: teamsPerGroup, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup must be between 4 and 32.");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        public void Should_NotHaveError_When_TeamsPerGroup_IsInValidRange_ForGroupsAndBracket(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 16, teamsPerGroup: teamsPerGroup, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.TeamsPerGroup);
        }

        [Theory]
        [InlineData(3)]
        public void Should_HaveError_When_TeamsPerGroup_IsBelowMinimum_ForGroupsOnly(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 32, teamsPerGroup: teamsPerGroup, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        public void Should_HaveError_When_TeamsPerGroup_IsAboveMaximum_ForGroupsOnly(int teamsPerGroup)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 32, teamsPerGroup: teamsPerGroup, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup must be between 4 and 32.");
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerGroup_IsNotNull_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, teamsPerGroup: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup);
        }

        #endregion

        #region Format-Specific Rules - BracketOnly

        [Fact]
        public void Should_HaveError_When_TeamsPerBracket_IsNull_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 8, teamsPerBracket: null, teamsPerGroup: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket is required for BracketOnly format.");
        }

        [Fact]
        public void Should_HaveError_When_MaxTeams_NotEquals_TeamsPerBracket_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 16, teamsPerBracket: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("For BracketOnly format, MaxTeams must equal TeamsPerBracket.");
        }

        [Theory]
        [InlineData(6)]
        [InlineData(12)]
        public void Should_HaveError_When_MaxTeams_NotPowerOfTwo_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams, teamsPerBracket: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("For BracketOnly format, MaxTeams must be a power of 2 (4, 8, 16, 32).");
        }

        [Fact]
        public void Should_NotHaveError_When_MaxTeams_Equals_TeamsPerBracket_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 8, teamsPerBracket: 8, teamsPerGroup: null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion

        #region Format-Specific Rules - GroupsAndBracket

        [Fact]
        public void Should_HaveError_When_TeamsPerGroup_IsNull_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 16, teamsPerGroup: null, teamsPerBracket: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup is required for GroupsAndBracket format.");
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerBracket_IsNull_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 16, teamsPerGroup: 4, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket is required for GroupsAndBracket format.");
        }

        [Fact]
        public void Should_HaveError_When_MaxTeams_NotDivisibleBy_TeamsPerGroup_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 10, teamsPerGroup: 4, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("MaxTeams must be divisible by TeamsPerGroup.");
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerGroup_GreaterThan_MaxTeams_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 8, teamsPerGroup: 16, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup cannot be greater than MaxTeams.");
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerBracket_GreaterThan_MaxTeams_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 8, teamsPerGroup: 4, teamsPerBracket: 16);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket cannot be greater than MaxTeams.");
        }

        [Theory]
        [InlineData(6)]
        [InlineData(12)]
        public void Should_HaveError_When_TeamsPerBracket_NotPowerOfTwo_ForGroupsAndBracket(int teamsPerBracket)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 16, teamsPerGroup: 4, teamsPerBracket: teamsPerBracket);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("For GroupsAndBracket format, TeamsPerBracket must be a power of 2 (4, 8, 16, 32).");
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerBracket_NotDivisibleBy_NumberOfGroups_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 16, teamsPerGroup: 4, teamsPerBracket: 9);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("TeamsPerBracket must be divisible by the number of groups (MaxTeams / TeamsPerGroup).");
        }

        [Fact]
        public void Should_NotHaveError_When_AllRulesValid_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 16, teamsPerGroup: 4, teamsPerBracket: 4);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion

        #region Format-Specific Rules - GroupsOnly

        [Fact]
        public void Should_HaveError_When_TeamsPerGroup_IsNull_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 16, teamsPerGroup: null, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerGroup)
                .WithErrorMessage("TeamsPerGroup is required for GroupsOnly format.");
        }

        [Fact]
        public void Should_HaveError_When_TeamsPerBracket_IsNotNull_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 16, teamsPerGroup: 4, teamsPerBracket: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.TeamsPerBracket)
                .WithErrorMessage("TeamsPerBracket should not be set for GroupsOnly format.");
        }

        [Fact]
        public void Should_HaveError_When_MaxTeams_NotDivisibleBy_TeamsPerGroup_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 10, teamsPerGroup: 4, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("MaxTeams must be divisible by TeamsPerGroup.");
        }

        [Fact]
        public void Should_NotHaveError_When_AllRulesValid_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 16, teamsPerGroup: 4, teamsPerBracket: null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion
    }
}
