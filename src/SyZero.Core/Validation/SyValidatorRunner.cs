using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Dependency;

namespace SyZero
{
    /// <summary>
    /// <see cref="ISyValidatorRunner"/> 默认实现：先尝试解析 <see cref="ISyValidator{T}"/>，否则回退到 DataAnnotations。
    /// </summary>
    public class SyValidatorRunner : ISyValidatorRunner, ISingletonDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DataAnnotationsValidator _fallback;

        /// <summary>
        /// 构造校验执行器。
        /// </summary>
        public SyValidatorRunner(IServiceProvider serviceProvider, DataAnnotationsValidator fallback)
        {
            _serviceProvider = serviceProvider;
            _fallback = fallback;
        }

        /// <inheritdoc/>
        public async Task<SyValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default)
        {
            if (instance is null)
            {
                return SyValidationResult.Invalid(null!, "对象不能为 null");
            }

            var validatorType = typeof(ISyValidator<>).MakeGenericType(instance.GetType());
            var validator = _serviceProvider.GetService(validatorType);
            if (validator is null)
            {
                return await _fallback.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
            }

            var method = validatorType.GetMethod(nameof(ISyValidator<object>.ValidateAsync));
            var task = (Task<SyValidationResult>)method!.Invoke(validator, new[] { instance, cancellationToken })!;
            return await task.ConfigureAwait(false);
        }
    }
}
