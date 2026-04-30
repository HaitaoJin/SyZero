using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Domain.Repository
{
    public interface IBaseRepository<T, in TKey> where T : class
    {
        #region Count

        /// <summary>
        ///     计数
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns></returns>
        int Count(Expression<Func<T, bool>> where);
        /// <summary>
        ///     计数
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<int> CountAsync(Expression<Func<T, bool>> where,
            CancellationToken cancellationToken = new CancellationToken());
        #endregion

        #region Select
        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="id">主键</param>
        /// <returns></returns>
        T GetModel(TKey id);
        /// <summary>
        /// 获取对象（异步）
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<T> GetModelAsync(TKey id, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns></returns>
        T GetModel(Expression<Func<T, bool>> where);
        /// <summary>
        /// 获取对象（异步）
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<T> GetModelAsync(Expression<Func<T, bool>> where,
            CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// 获取所有数据
        /// </summary>
        /// <returns></returns>
        IQueryable<T> GetList();
        /// <summary>
        /// 获取所有数据（异步）
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IQueryable<T>> GetListAsync(CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// 获取部分数据
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns></returns>
        IQueryable<T> GetList(Expression<Func<T, bool>> where);
        /// <summary>
        /// 获取部分数据（异步）
        /// </summary>
        /// <param name="where"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IQueryable<T>> GetListAsync(Expression<Func<T, bool>> where,
            CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// 分页获取数据
        /// </summary>
        /// <typeparam name="TProperty">属性</typeparam>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">每页条数</param>
        /// <param name="sortBy">排序</param>
        /// <param name="isDesc"></param>
        /// <returns></returns>
        IQueryable<T> GetPaged(int pageIndex, int pageSize, Expression<Func<T, object>> sortBy, bool isDesc = false);
        /// <summary>
        /// 分页获取数据（异步）
        /// </summary>
        /// <typeparam name="TProperty">属性</typeparam>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="sortBy">排序</param>
        /// <param name="isDesc"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<IQueryable<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, object>> sortBy, bool isDesc = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// 分页获取部分数据
        /// </summary>
        /// <typeparam name="TProperty">属性</typeparam>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">每页条数</param>
        /// <param name="sortBy">排序</param>
        /// <param name="where">条件</param>
        /// <param name="isDesc"></param>
        /// <returns></returns>
        IQueryable<T> GetPaged(int pageIndex, int pageSize, Expression<Func<T, object>> sortBy,
            Expression<Func<T, bool>> where, bool isDesc = false);
        /// <summary>
        /// 分页获取部分数据（异步）
        /// </summary>
        /// <typeparam name="TProperty">属性</typeparam>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="sortBy">排序</param>
        /// <param name="where">条件</param>
        /// <param name="isDesc"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<IQueryable<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, object>> sortBy,
            Expression<Func<T, bool>> where, bool isDesc = false, CancellationToken cancellationToken = default(CancellationToken));
        #endregion

        #region Insert
        /// <summary>
        ///     添加一条数据
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        T Add(T entity);
        /// <summary>
        ///     添加一条数据（异步）
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<T> AddAsync(T entity,
            CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        ///     添加多条数据
        /// </summary>
        /// <param name="entities"></param>
        int AddList(IQueryable<T> entities);
        /// <summary>
        ///     添加多条数据（异步）
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<int> AddListAsync(IQueryable<T> entities,
            CancellationToken cancellationToken = default(CancellationToken));
        #endregion

        #region Update
        /// <summary>
        ///     更新一条数据
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        long Update(T entity);
        /// <summary>
        ///     更新一条数据（异步）
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<long> UpdateAsync(T entity,
            CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        ///     更新多条数据
        /// </summary>
        /// <param name="entitys"></param>
        /// <returns></returns>
        long Update(IQueryable<T> entitys);
        /// <summary>
        ///     更新多条数据（异步）
        /// </summary>
        /// <param name="entitys"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<long> UpdateAsync(IQueryable<T> entitys,
            CancellationToken cancellationToken = default(CancellationToken));
        #endregion

        #region Delete
        /// <summary>
        ///     根据主键ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        long Delete(TKey id);
        /// <summary>
        ///     根据主键ID（异步）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<long> DeleteAsync(TKey id,
            CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        ///     删除
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns></returns>
        long Delete(Expression<Func<T, bool>> where);
        /// <summary>
        ///     删除（异步）
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="cancellationToken">异步取消凭据</param>
        /// <returns></returns>
        Task<long> DeleteAsync(Expression<Func<T, bool>> where,
            CancellationToken cancellationToken = default(CancellationToken));
        #endregion
    }
}
