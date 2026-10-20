using Application.DTOs.Team;
using Application.Validations.Team;
using FluentValidation.TestHelper;

namespace Tests.Unit.Validators
{
    public class AddTeamsValidatorTests
    {
        private readonly AddTeamsValidator _validator = new();

        private static AddTeamsRequestDTO Names(params string[] names) => new(names.ToList());

        [Fact]
        public void Should_NotHaveError_When_NamesValid()
        {
            var result = _validator.TestValidate(Names("Alpha", " Bravo ", new string('c', 24)));
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_When_NoNames()
        {
            _validator.TestValidate(Names()).ShouldHaveValidationErrorFor(x => x.Names);
            _validator.TestValidate(new AddTeamsRequestDTO(null!)).ShouldHaveValidationErrorFor(x => x.Names);
        }

        [Fact]
        public void Should_HaveError_When_MoreThan32Names()
        {
            var names = Enumerable.Range(1, 33).Select(i => $"Team {i}").ToArray();
            _validator.TestValidate(Names(names)).ShouldHaveValidationErrorFor(x => x.Names);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_HaveError_When_NameEmpty(string name)
        {
            _validator.TestValidate(Names("Alpha", name)).ShouldHaveValidationErrorFor("Names[1]");
        }

        [Fact]
        public void Should_HaveError_When_NameTooLongAfterTrim()
        {
            _validator.TestValidate(Names(new string('a', 25))).ShouldHaveValidationErrorFor("Names[0]");
            _validator.TestValidate(Names($"  {new string('a', 24)}  ")).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_When_DuplicateIgnoringCaseAndSpaces()
        {
            _validator.TestValidate(Names("Alpha", " ALPHA")).ShouldHaveValidationErrorFor(x => x.Names);
        }
    }
}
