using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.BackgroundJobs;

public enum BackgroundJobStatus { Queued, Running, Succeeded, RetryScheduled, DeadLettered }

public sealed record BackgroundJobState(
    string Id, string JobType, BackgroundJobStatus Status, int Attempt,
    DateTime EnqueuedAtUtc, DateTime UpdatedAtUtc, string? LastError = null);

public interface IBackgroundJobStateStore
{
    Task SaveAsync(BackgroundJobState state, CancellationToken cancellationToken = default);
    Task<BackgroundJobState?> FindAsync(string id, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BackgroundJobState> GetDeadLettersAsync(CancellationToken cancellationToken = default);
}

public sealed class MemoryBackgroundJobStateStore : IBackgroundJobStateStore
{
    private readonly ConcurrentDictionary<string, BackgroundJobState> _states = new();
    public Task SaveAsync(BackgroundJobState state, CancellationToken cancellationToken = default)
    { _states[state.Id] = state; return Task.CompletedTask; }
    public Task<BackgroundJobState?> FindAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_states.TryGetValue(id, out var state) ? state : null);
    public async IAsyncEnumerable<BackgroundJobState> GetDeadLettersAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var state in _states.Values.Where(x => x.Status == BackgroundJobStatus.DeadLettered).OrderBy(x => x.UpdatedAtUtc))
        { cancellationToken.ThrowIfCancellationRequested(); yield return state; await Task.Yield(); }
    }
}
