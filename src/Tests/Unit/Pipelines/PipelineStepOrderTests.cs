using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
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
            { GameResultPipeline.StepOrder, typeof(IGameResultPipelineStep) },
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
