using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SyZero.DistributedTransaction.Saga
{
    /// <summary>
    /// Saga 步骤：包含正向动作 <see cref="ExecuteAsync"/> 与补偿动作 <see cref="CompensateAsync"/>。
    /// </summary>
    public interface ISagaStep<TContext>
    {
        string Name { get; }
        Task ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
        Task CompensateAsync(TContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 简易 Saga 编排器：顺序执行步骤，任一步失败则按已执行步骤的反向顺序补偿。
    /// </summary>
    public class SagaCoordinator<TContext>
    {
        private readonly List<ISagaStep<TContext>> _steps = new List<ISagaStep<TContext>>();
        private readonly ILogger? _logger;

        public SagaCoordinator(ILogger? logger = null)
        {
            _logger = logger;
        }

        public SagaCoordinator<TContext> Add(ISagaStep<TContext> step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            _steps.Add(step);
            return this;
        }

        public async Task<SagaResult> RunAsync(TContext context, CancellationToken cancellationToken = default)
        {
            var executed = new Stack<ISagaStep<TContext>>();
            try
            {
                foreach (var step in _steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger?.LogInformation("Saga 执行步骤 {Step}", step.Name);
                    await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                    executed.Push(step);
                }
                return SagaResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Saga 步骤失败，开始补偿");
                var compensationErrors = new List<string>();
                while (executed.Count > 0)
                {
                    var step = executed.Pop();
                    try
                    {
                        _logger?.LogInformation("Saga 补偿步骤 {Step}", step.Name);
                        await step.CompensateAsync(context, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception compEx)
                    {
                        _logger?.LogError(compEx, "Saga 补偿步骤 {Step} 失败", step.Name);
                        compensationErrors.Add($"{step.Name}: {compEx.Message}");
                    }
                }
                return SagaResult.Failure(ex.Message, compensationErrors);
            }
        }
    }

    /// <summary>
    /// Saga 执行结果。
    /// </summary>
    public class SagaResult
    {
        public bool IsSuccess { get; private set; }
        public string? Error { get; private set; }
        public IReadOnlyList<string> CompensationErrors { get; private set; } = Array.Empty<string>();

        public static SagaResult Success() => new SagaResult { IsSuccess = true };
        public static SagaResult Failure(string error, IReadOnlyList<string>? compensationErrors = null) => new SagaResult
        {
            IsSuccess = false,
            Error = error,
            CompensationErrors = compensationErrors ?? Array.Empty<string>()
        };
    }
}
