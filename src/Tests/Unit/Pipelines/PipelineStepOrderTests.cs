using Application.Pipelines.MatchResult;
using Application.Pipelines.MatchResult.Contracts;
using Application.Pipelines.StartBracket;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartGroups;
using Application.Pipelines.StartGroups.Contracts;
using FluentAssertions;

namespace Tests.Unit.Pipelines
{
    public class PipelineStepOrderTests
    {
        public static TheoryData<Type[], Type> StepOrders => new()
        {
            { MatchResultPipeline.StepOrder, typeof(IMatchResultPipelineStep) },
            { StartGroupsPipeline.StepOrder, typeof(IStartGroupsPipelineStep) },
            { StartBracketPipeline.StepOrder, typeof(IStartBracketPipelineStep) },
        };

        [Theory]
        [MemberData(nameof(StepOrders))]
        public void StepOrder_Should_ListDistinctStepsOfThePipeline(Type[] stepOrder, Type stepInterface)
        {
            stepOrder.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
            stepOrder.Should().AllSatisfy(t => t.Should().BeAssignableTo(stepInterface));
        }
    }
}
