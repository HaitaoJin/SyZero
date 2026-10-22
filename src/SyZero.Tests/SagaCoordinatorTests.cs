using System.Threading;
using System.Threading.Tasks;
using SyZero.DistributedTransaction.Saga;
using Xunit;

namespace SyZero.Tests;

public class SagaCoordinatorTests
{
    private class Step : ISagaStep<System.Collections.Generic.List<string>>
    {
        public string Name { get; }
        private readonly bool _throwOnExecute;
        public Step(string name, bool throwOnExecute = false) { Name = name; _throwOnExecute = throwOnExecute; }
        public Task ExecuteAsync(System.Collections.Generic.List<string> ctx, CancellationToken ct = default)
        {
            ctx.Add($"exec:{Name}");
            if (_throwOnExecute) throw new System.InvalidOperationException("fail-" + Name);
            return Task.CompletedTask;
        }
        public Task CompensateAsync(System.Collections.Generic.List<string> ctx, CancellationToken ct = default)
        {
            ctx.Add($"comp:{Name}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SuccessfulRun_ExecutesAllSteps_NoCompensation()
    {
        var ctx = new System.Collections.Generic.List<string>();
        var saga = new SagaCoordinator<System.Collections.Generic.List<string>>().Add(new Step("A")).Add(new Step("B"));
        var result = await saga.RunAsync(ctx);
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "exec:A", "exec:B" }, ctx);
    }

    [Fact]
    public async Task FailedStep_TriggersReverseCompensation()
    {
        var ctx = new System.Collections.Generic.List<string>();
        var saga = new SagaCoordinator<System.Collections.Generic.List<string>>()
            .Add(new Step("A"))
            .Add(new Step("B"))
            .Add(new Step("C", throwOnExecute: true));
        var result = await saga.RunAsync(ctx);
        Assert.False(result.IsSuccess);
        // C 失败：A、B 已执行，反向补偿 B、A（C 抛出未推入栈不会补偿）
        Assert.Equal(new[] { "exec:A", "exec:B", "exec:C", "comp:B", "comp:A" }, ctx);
    }
}
