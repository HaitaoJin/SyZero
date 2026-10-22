using System;
using System.Threading.Tasks;

namespace SyZero.Domain.Repository
{
    /// <summary>
    /// 事务作用域接口
    /// </summary>
    public interface ITransactionScope : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// 提交事务
        /// </summary>
        void Commit();

        /// <summary>
        /// 提交事务（异步）
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        void Rollback();

        /// <summary>
        /// 回滚事务（异步）
        /// </summary>
        Task RollbackAsync();
    }

    public interface IUnitOfWork
    {
        /// <summary>
        /// 开启事务
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// 回滚事务
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// 释放事务
        /// </summary>
        void DisposeTransaction();

        /// <summary>
        /// 开启事务（异步），返回事务作用域
        /// </summary>
        /// <returns>事务作用域，可用于提交或回滚</returns>
        Task<ITransactionScope> BeginTransactionAsync();

        /// <summary>
        /// 在事务中执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        void ExecuteInTransaction(Action action);

        /// <summary>
        /// 在事务中执行操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <returns>操作结果</returns>
        T ExecuteInTransaction<T>(Func<T> func);

        /// <summary>
        /// 在事务中执行异步操作
        /// </summary>
        /// <param name="func">要执行的异步操作</param>
        /// <returns></returns>
        Task ExecuteInTransactionAsync(Func<Task> func);

        /// <summary>
        /// 在事务中执行异步操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的异步操作</param>
        /// <returns>操作结果</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> func);
    }
}
