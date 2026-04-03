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

        private CreateTournamentRequestDTO CreateValidDTO(
            string? name = null,
            string? description = null,
            int? maxTeams = null,
            Format? format = null,
            int? numberOfGroups = null,
            int? advancingPerGroup = null)
        {
            return new CreateTournamentRequestDTO(
                Name: name ?? "Test Tournament",
                Description: description ?? "Test Description",
                MaxTeams: maxTeams ?? 12,
                Format: format ?? Format.GroupsAndBracket,
                NumberOfGroups: numberOfGroups,
                AdvancingPerGroup: advancingPerGroup
            );
        }

        #region Name Validation

        [Fact]
        public void Should_HaveError_When_Name_IsEmpty()
        {
            var request = CreateValidDTO(name: "", format: Format.BracketOnly, maxTeams: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("A")]
        public void Should_HaveError_When_Name_IsTooShort(string name)
        {
            var request = CreateValidDTO(name: name, format: Format.BracketOnly, maxTeams: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_HaveError_When_Name_IsTooLong()
        {
            var request = CreateValidDTO(name: new string('A', 101), format: Format.BracketOnly, maxTeams: 8);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData("Test")]
        [InlineData("Valid Tournament Name")]
        public void Should_NotHaveError_When_Name_IsValid(string name)
        {
            var request = CreateValidDTO(name: name, format: Format.GroupsOnly, maxTeams: 9, numberOfGroups: 3);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Description Validation

        [Fact]
        public void Should_HaveError_When_Description_ExceedsMaxLength()
        {
            var request = CreateValidDTO(description: new string('A', 251), format: Format.BracketOnly, maxTeams: 8);
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
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        [InlineData(64)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(16)]
        [InlineData(32)]
        public void Should_NotHaveError_When_MaxTeams_IsInValidRange_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.MaxTeams);
        }

        [Theory]
        [InlineData(3)]
        public void Should_HaveError_When_MaxTeams_IsBelowMinimum_ForGroupsAndBracket(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: maxTeams, numberOfGroups: 1, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum_ForGroupsAndBracket(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: maxTeams, numberOfGroups: 2, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(3)]
        public void Should_HaveError_When_MaxTeams_IsBelowMinimum_ForGroupsOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: maxTeams, numberOfGroups: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        [Theory]
        [InlineData(33)]
        public void Should_HaveError_When_MaxTeams_IsAboveMaximum_ForGroupsOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: maxTeams, numberOfGroups: 2);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.MaxTeams)
                .WithErrorMessage("MaxTeams must be between 4 and 32.");
        }

        #endregion

        #region BracketOnly Format

        [Fact]
        public void Should_NotHaveError_When_MaxTeams_NotPowerOfTwo_ForBracketOnly()
        {
            // Non-power-of-2 values are now valid for BracketOnly
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 6);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.MaxTeams);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(13)]
        public void Should_NotHaveError_When_MaxTeams_IsAnyValidInt_ForBracketOnly(int maxTeams)
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: maxTeams);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.MaxTeams);
        }

        [Fact]
        public void Should_HaveError_When_NumberOfGroups_IsSet_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 8, numberOfGroups: 2);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.NumberOfGroups);
        }

        [Fact]
        public void Should_HaveError_When_AdvancingPerGroup_IsSet_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 8, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.AdvancingPerGroup);
        }

        [Fact]
        public void Should_NotHaveError_When_AllRulesValid_ForBracketOnly()
        {
            var request = CreateValidDTO(format: Format.BracketOnly, maxTeams: 8);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.MaxTeams);
            result.ShouldNotHaveValidationErrorFor(x => x.NumberOfGroups);
            result.ShouldNotHaveValidationErrorFor(x => x.AdvancingPerGroup);
        }

        #endregion

        #region GroupsAndBracket Format

        [Fact]
        public void Should_HaveError_When_NumberOfGroups_IsNull_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 12, numberOfGroups: null, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.NumberOfGroups)
                .WithErrorMessage("NumberOfGroups is required for GroupsAndBracket format.");
        }

        [Fact]
        public void Should_HaveError_When_AdvancingPerGroup_IsNull_ForGroupsAndBracket()
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 12, numberOfGroups: 3, advancingPerGroup: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.AdvancingPerGroup)
                .WithErrorMessage("AdvancingPerGroup is required for GroupsAndBracket format.");
        }

        [Theory]
        [InlineData(12, 5)]  // floor(12/5) = 2 < 3
        [InlineData(10, 4)]  // floor(10/4) = 2 < 3
        public void Should_HaveError_When_MinGroupSize_BelowThree_ForGroupsAndBracket(int maxTeams, int numberOfGroups)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: maxTeams, numberOfGroups: numberOfGroups, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.NumberOfGroups)
                .WithErrorMessage("Each group must have at least 3 teams (MaxTeams / NumberOfGroups >= 3).");
        }

        [Theory]
        [InlineData(12, 4, 1)]  // floor(12/4) = 3, advancingPerGroup=1 < 3 ✓
        [InlineData(12, 3, 3)]  // floor(12/3) = 4, advancingPerGroup=3 = minGroupSize → NOT < minGroupSize
        public void Should_Validate_AdvancingPerGroup_Bounds_ForGroupsAndBracket(int maxTeams, int numberOfGroups, int advancingPerGroup)
        {
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: maxTeams, numberOfGroups: numberOfGroups, advancingPerGroup: advancingPerGroup);
            var result = _validator.TestValidate(request);
            // advancingPerGroup=3 with minGroupSize=4: 3 < 4 ✓ valid
            // advancingPerGroup=3 with minGroupSize=3: 3 < 3 ✗ invalid
            bool shouldFail = advancingPerGroup >= (maxTeams / numberOfGroups);
            if (shouldFail)
                result.ShouldHaveValidationErrorFor(x => x.AdvancingPerGroup);
            else
                result.ShouldNotHaveValidationErrorFor(x => x.AdvancingPerGroup);
        }

        [Fact]
        public void Should_HaveError_When_AdvancingPerGroup_EqualsMinGroupSize_ForGroupsAndBracket()
        {
            // 12 teams / 3 groups = 4 per group; advancingPerGroup=4 means 0 eliminated → invalid
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 12, numberOfGroups: 3, advancingPerGroup: 4);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.AdvancingPerGroup)
                .WithErrorMessage("AdvancingPerGroup must be less than the minimum group size (at least 1 team eliminated per group).");
        }

        [Fact]
        public void Should_NotHaveError_When_AllRulesValid_ForGroupsAndBracket()
        {
            // 12 teams / 3 groups = 4 per group, advancing 2 per group → bracketSize=8
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 12, numberOfGroups: 3, advancingPerGroup: 2);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Should_NotHaveError_When_UnequalGroups_ForGroupsAndBracket()
        {
            // 11 teams / 3 groups = groups of 4/4/3 (min=3), advancing 1 per group
            var request = CreateValidDTO(format: Format.GroupsAndBracket, maxTeams: 11, numberOfGroups: 3, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion

        #region GroupsOnly Format

        [Fact]
        public void Should_HaveError_When_NumberOfGroups_IsNull_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 12, numberOfGroups: null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.NumberOfGroups)
                .WithErrorMessage("NumberOfGroups is required for GroupsOnly format.");
        }

        [Fact]
        public void Should_HaveError_When_AdvancingPerGroup_IsSet_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 12, numberOfGroups: 3, advancingPerGroup: 1);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.AdvancingPerGroup)
                .WithErrorMessage("AdvancingPerGroup should not be set for GroupsOnly format.");
        }

        [Theory]
        [InlineData(12, 5)]  // floor(12/5) = 2 < 3
        [InlineData(10, 4)]  // floor(10/4) = 2 < 3
        public void Should_HaveError_When_MinGroupSize_BelowThree_ForGroupsOnly(int maxTeams, int numberOfGroups)
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: maxTeams, numberOfGroups: numberOfGroups);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.NumberOfGroups)
                .WithErrorMessage("Each group must have at least 3 teams (MaxTeams / NumberOfGroups >= 3).");
        }

        [Fact]
        public void Should_NotHaveError_When_AllRulesValid_ForGroupsOnly()
        {
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 12, numberOfGroups: 4);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Should_NotHaveError_When_UnequalGroups_ForGroupsOnly()
        {
            // 11 teams / 3 groups = 4/4/3 (min=3 ✓)
            var request = CreateValidDTO(format: Format.GroupsOnly, maxTeams: 11, numberOfGroups: 3);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion
    }
}
