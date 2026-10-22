using System;

namespace SyZero.AspNetCore.Middleware
{
    /// <summary>
    /// SyZero 统一错误码模型，用于 RFC 7807 ProblemDetails 输出。
    /// </summary>
    public class SyError
    {
        /// <summary>
        /// 业务错误码（数字或字符串约定，由业务统一管理）。
        /// </summary>
        public string Code { get; set; } = "SY-0";

        /// <summary>
        /// 简短标题。
        /// </summary>
        public string Title { get; set; } = "Internal Server Error";

        /// <summary>
        /// 详细信息。
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// 关联类型 URI，建议使用文档链接。
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// HTTP 状态码（默认 500）。
        /// </summary>
        public int Status { get; set; } = 500;

        public static SyError Internal(string? detail = null) => new SyError { Code = "SY-500", Title = "Internal Server Error", Status = 500, Detail = detail };
        public static SyError BadRequest(string? detail = null) => new SyError { Code = "SY-400", Title = "Bad Request", Status = 400, Detail = detail };
        public static SyError Unauthorized(string? detail = null) => new SyError { Code = "SY-401", Title = "Unauthorized", Status = 401, Detail = detail };
        public static SyError Forbidden(string? detail = null) => new SyError { Code = "SY-403", Title = "Forbidden", Status = 403, Detail = detail };
        public static SyError NotFound(string? detail = null) => new SyError { Code = "SY-404", Title = "Not Found", Status = 404, Detail = detail };
        public static SyError Conflict(string? detail = null) => new SyError { Code = "SY-409", Title = "Conflict", Status = 409, Detail = detail };
        public static SyError TooManyRequests(string? detail = null) => new SyError { Code = "SY-429", Title = "Too Many Requests", Status = 429, Detail = detail };
    }

    /// <summary>
    /// 业务异常：携带 <see cref="SyError"/>，由 <see cref="SyExceptionMiddleware"/> 转换为 ProblemDetails。
    /// </summary>
    [Serializable]
    public class SyBusinessException : Exception
    {
        public SyError Error { get; }

        public SyBusinessException(SyError error)
            : base(error?.Detail ?? error?.Title ?? "Business error")
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        public SyBusinessException(string code, string title, int status = 400, string? detail = null)
            : this(new SyError { Code = code, Title = title, Status = status, Detail = detail })
        {
        }
    }
}
