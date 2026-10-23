using System;
using System.Threading.Tasks;

namespace SyZero.Domain.Repository;

/// <summary>
/// 未安装持久化模块时的工作单元占位实现，使网关等无数据库宿主可独立启动。
/// </summary>
internal sealed class UnsupportedUnitOfWork : IUnitOfWork
{
    private static InvalidOperationException CreateException()
        => new("尚未注册工作单元实现，请添加 SyZero.EntityFrameworkCore、SyZero.SqlSugar 或其他持久化模块。");

    public void BeginTransaction() => throw CreateException();
    public void CommitTransaction() => throw CreateException();
    public void RollbackTransaction() => throw CreateException();
    public void DisposeTransaction() => throw CreateException();
    public Task<ITransactionScope> BeginTransactionAsync() => Task.FromException<ITransactionScope>(CreateException());
    public void ExecuteInTransaction(Action action) => throw CreateException();
    public T ExecuteInTransaction<T>(Func<T> func) => throw CreateException();
    public Task ExecuteInTransactionAsync(Func<Task> func) => Task.FromException(CreateException());
    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> func) => Task.FromException<T>(CreateException());
}
