using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Validation
{
    /// <summary>
    /// SyZero 校验器抽象。业务可基于此适配 FluentValidation 等任意校验库。
    /// </summary>
    /// <typeparam name="T">被校验的对象类型。</typeparam>
    public interface ISyValidator<in T>
    {
        /// <summary>
        /// 校验对象，返回结果（<see cref="SyValidationResult"/>）。
        /// </summary>
        Task<SyValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 非泛型校验器入口，基于运行时类型分派。
    /// </summary>
    public interface ISyValidatorRunner
    {
        /// <summary>
        /// 校验任意对象，自动定位 <see cref="ISyValidator{T}"/> 实现。
        /// 若未注册对应校验器，则使用内置 <see cref="DataAnnotationsValidator"/> 兜底。
        /// </summary>
        Task<SyValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default);
    }
}
