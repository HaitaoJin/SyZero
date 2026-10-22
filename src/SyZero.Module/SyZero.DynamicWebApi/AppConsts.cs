using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SyZero.DynamicWebApi
{
    /// <summary>
    /// Dynamic WebApi 常量和默认配置
    /// </summary>
    public static class AppConsts
    {
        /// <summary>
        /// 默认 HTTP 动词映射（基于方法名前缀）
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> DefaultHttpVerbMappings = 
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["add"] = "POST",
                ["create"] = "POST",
                ["post"] = "POST",
                ["insert"] = "POST",
                ["save"] = "POST",

                ["get"] = "GET",
                ["find"] = "GET",
                ["fetch"] = "GET",
                ["query"] = "GET",
                ["search"] = "GET",
                ["list"] = "GET",
                ["load"] = "GET",

                ["update"] = "PUT",
                ["put"] = "PUT",
                ["modify"] = "PUT",
                ["edit"] = "PUT",

                ["patch"] = "PATCH",

                ["delete"] = "DELETE",
                ["remove"] = "DELETE",
            });

        /// <summary>
        /// 默认控制器后缀（将被移除）
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultControllerPostfixes = 
            new ReadOnlyCollection<string>(new List<string> { "AppService", "ApplicationService", "Service" });

        /// <summary>
        /// 默认 Action 后缀（将被移除）
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultActionPostfixes = 
            new ReadOnlyCollection<string>(new List<string> { "Async" });

        /// <summary>
        /// 默认 API 前缀
        /// </summary>
        public const string DefaultApiPrefix = "api";

        /// <summary>
        /// 默认 HTTP 动词
        /// </summary>
        public const string DefaultHttpVerb = "POST";
    }
}