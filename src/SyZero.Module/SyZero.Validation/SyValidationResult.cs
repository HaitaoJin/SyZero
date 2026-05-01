using System.Collections.Generic;
using System.Linq;

namespace SyZero.Validation
{
    /// <summary>
    /// 单条校验错误。
    /// </summary>
    public sealed class SyValidationError
    {
        /// <summary>
        /// 错误所属字段名。可能为空（对象级错误）。
        /// </summary>
        public string? Field { get; }

        /// <summary>
        /// 错误描述。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 创建一个校验错误。
        /// </summary>
        public SyValidationError(string? field, string message)
        {
            Field = field;
            Message = message;
        }
    }

    /// <summary>
    /// 校验结果聚合。
    /// </summary>
    public sealed class SyValidationResult
    {
        private readonly List<SyValidationError> _errors;

        /// <summary>
        /// 错误列表。<see cref="IsValid"/> 为 true 时为空集合。
        /// </summary>
        public IReadOnlyList<SyValidationError> Errors => _errors;

        /// <summary>
        /// 是否校验通过。
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        private SyValidationResult(List<SyValidationError> errors)
        {
            _errors = errors;
        }

        /// <summary>
        /// 校验通过的结果。
        /// </summary>
        public static SyValidationResult Valid { get; } = new SyValidationResult(new List<SyValidationError>());

        /// <summary>
        /// 创建一个包含若干错误的校验结果。
        /// </summary>
        public static SyValidationResult Invalid(IEnumerable<SyValidationError> errors)
            => new SyValidationResult(errors.ToList());

        /// <summary>
        /// 便捷构造：单条错误。
        /// </summary>
        public static SyValidationResult Invalid(string field, string message)
            => new SyValidationResult(new List<SyValidationError> { new SyValidationError(field, message) });
    }
}
