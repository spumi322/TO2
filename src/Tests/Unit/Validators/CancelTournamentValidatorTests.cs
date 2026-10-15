using Application.DTOs.Tournament;
using Application.Validations.Tournament;
using FluentValidation.TestHelper;

namespace Tests.Unit.Validators
{
    public class CancelTournamentValidatorTests
    {
        private readonly CancelTournamentValidator _validator = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Venue closed")]
        public void Should_NotHaveError_When_Reason_IsOptionalOrShort(string? reason)
        {
            var result = _validator.TestValidate(new CancelTournamentRequestDTO(reason));
            result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Should_HaveError_When_Reason_TooLong()
        {
            var result = _validator.TestValidate(new CancelTournamentRequestDTO(new string('a', 251)));
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }
    }
}
