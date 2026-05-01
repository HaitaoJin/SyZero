using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Validation
{
    /// <summary>
    /// 基于 <c>System.ComponentModel.DataAnnotations</c> 的兜底校验器。
    /// 不依赖任何第三方校验库，对包含 <see cref="ValidationAttribute"/> 标注的属性进行检查。
    /// </summary>
    public class DataAnnotationsValidator : ISingletonDependency
    {
        /// <summary>
        /// 同步校验对象。
        /// </summary>
        public SyValidationResult Validate(object instance)
        {
            if (instance is null)
            {
                return SyValidationResult.Invalid(null!, "对象不能为 null");
            }

            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            if (Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true))
            {
                return SyValidationResult.Valid;
            }

            var errors = results.SelectMany(r =>
            {
                var members = r.MemberNames?.ToList() ?? new List<string>();
                if (members.Count == 0)
                {
                    return new[] { new SyValidationError(null, r.ErrorMessage ?? "校验失败") };
                }
                return members.Select(m => new SyValidationError(m, r.ErrorMessage ?? "校验失败"));
            });

            return SyValidationResult.Invalid(errors);
        }

        /// <summary>
        /// 异步入口（仅包装同步实现）。
        /// </summary>
        public Task<SyValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Validate(instance));
        }
    }
}
