using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyZero
{
    /// <summary>
    /// SyZero.Validation 扩展方法。
    /// </summary>
    public static class SyZeroValidationExtension
    {
        /// <summary>
        /// 注册 SyZero 校验框架（默认包含 DataAnnotations 兜底）。
        /// 业务可通过 <c>services.AddTransient&lt;ISyValidator&lt;MyDto&gt;, MyDtoValidator&gt;()</c> 注册具体校验器。
        /// </summary>
        public static IServiceCollection AddSyZeroValidation(this IServiceCollection services)
        {
            services.TryAddSingleton<DataAnnotationsValidator>();
            services.TryAddSingleton<ISyValidatorRunner, SyValidatorRunner>();
            return services;
        }
    }
}
