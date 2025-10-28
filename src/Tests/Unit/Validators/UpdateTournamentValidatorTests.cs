using Application.DTOs.Tournament;
using Application.Validations.Tournament;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Tests.Unit.Validators
{
    public class UpdateTournamentValidatorTests
    {
        private readonly UpdateTournamentValidator _validator;

        public UpdateTournamentValidatorTests()
        {
            _validator = new UpdateTournamentValidator();
        }

        private UpdateTournamentRequestDTO CreateValidDTO(
            string? name = null,
            string? description = null,
            TournamentStatus? status = null)
        {
            return new UpdateTournamentRequestDTO(
                Name: name ?? "Test Tournament",
                Description: description ?? "Test Description",
                status: status ?? TournamentStatus.Setup
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

        [Fact]
        public void Should_NotHaveError_When_Description_IsAtMaxLength()
        {
            var request = CreateValidDTO(description: new string('A', 250));
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        #endregion
    }
}
