using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SyZero.DynamicWebApi
{
    /// <summary>
    /// Dynamic WebApi 配置选项
    /// </summary>
    public class DynamicWebApiOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "DynamicWebApi";

        /// <summary>
        /// 初始化默认配置
        /// </summary>
        public DynamicWebApiOptions()
        {
            RemoveControllerPostfixes = new List<string>(AppConsts.DefaultControllerPostfixes);
            RemoveActionPostfixes = new List<string>(AppConsts.DefaultActionPostfixes);
            FormBodyBindingIgnoredTypes = new List<Type> { typeof(IFormFile) };
            DefaultHttpVerb = AppConsts.DefaultHttpVerb;
            DefaultApiPrefix = AppConsts.DefaultApiPrefix;
            AssemblyDynamicWebApiOptions = new Dictionary<Assembly, AssemblyDynamicWebApiOptions>();
            HttpVerbMappings = new Dictionary<string, string>(AppConsts.DefaultHttpVerbMappings, StringComparer.OrdinalIgnoreCase);
            EnableLowerCaseRoutes = true;
        }

        /// <summary>
        /// API HTTP 动词
        /// <para>默认值: "POST"</para>
        /// </summary>
        public string DefaultHttpVerb { get; set; }

        /// <summary>
        /// 默认区域名称
        /// </summary>
        public string DefaultAreaName { get; set; }

        /// <summary>
        /// API 路由前缀
        /// <para>默认值: "api"</para>
        /// </summary>
        public string DefaultApiPrefix { get; set; }

        /// <summary>
        /// 需要移除的控制器名称后缀
        /// <para>默认值: {"AppService", "ApplicationService", "Service"}</para>
        /// </summary>
        public List<string> RemoveControllerPostfixes { get; set; }

        /// <summary>
        /// 需要移除的 Action 名称后缀
        /// <para>默认值: {"Async"}</para>
        /// </summary>
        public List<string> RemoveActionPostfixes { get; set; }

        /// <summary>
        /// 忽略 Form Body 绑定的类型
        /// <para>默认值: {IFormFile}</para>
        /// </summary>
        public List<Type> FormBodyBindingIgnoredTypes { get; set; }

        /// <summary>
        /// 自定义 Action 名称处理函数
        /// </summary>
        public Func<string, string> GetRestFulActionName { get; set; }

        /// <summary>
        /// 程序集级别的 Dynamic WebApi 选项
        /// </summary>
        public Dictionary<Assembly, AssemblyDynamicWebApiOptions> AssemblyDynamicWebApiOptions { get; }

        /// <summary>
        /// HTTP 动词映射（基于方法名前缀）
        /// <para>例如: "Get" -> "GET", "Create" -> "POST"</para>
        /// </summary>
        public Dictionary<string, string> HttpVerbMappings { get; set; }

        /// <summary>
        /// 是否启用小写路由
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableLowerCaseRoutes { get; set; }

        /// <summary>
        /// 验证所有配置是否有效
        /// </summary>
        /// <exception cref="ArgumentException">配置无效时抛出</exception>
        public void Validate()
        {
            if (string.IsNullOrEmpty(DefaultHttpVerb))
            {
                throw new ArgumentException($"{nameof(DefaultHttpVerb)} 不能为空。");
            }

            DefaultAreaName ??= string.Empty;
            DefaultApiPrefix ??= string.Empty;

            if (FormBodyBindingIgnoredTypes == null)
            {
                throw new ArgumentException($"{nameof(FormBodyBindingIgnoredTypes)} 不能为 null。");
            }

            if (RemoveControllerPostfixes == null)
            {
                throw new ArgumentException($"{nameof(RemoveControllerPostfixes)} 不能为 null。");
            }

            if (RemoveActionPostfixes == null)
            {
                throw new ArgumentException($"{nameof(RemoveActionPostfixes)} 不能为 null。");
            }

            if (HttpVerbMappings == null)
            {
                throw new ArgumentException($"{nameof(HttpVerbMappings)} 不能为 null。");
            }
        }

        /// <summary>
        /// 添加程序集级别的 Dynamic WebApi 选项
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <param name="apiPrefix">API 前缀</param>
        /// <param name="httpVerb">HTTP 动词</param>
        /// <exception cref="ArgumentNullException">assembly 为 null 时抛出</exception>
        public void AddAssemblyOptions(Assembly assembly, string apiPrefix = null, string httpVerb = null)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            AssemblyDynamicWebApiOptions[assembly] = new AssemblyDynamicWebApiOptions(apiPrefix, httpVerb);
        }

        /// <summary>
        /// 添加自定义 HTTP 动词映射
        /// </summary>
        /// <param name="methodPrefix">方法名前缀</param>
        /// <param name="httpVerb">HTTP 动词</param>
        public void AddHttpVerbMapping(string methodPrefix, string httpVerb)
        {
            if (string.IsNullOrEmpty(methodPrefix))
            {
                throw new ArgumentException($"{nameof(methodPrefix)} 不能为空。");
            }

            if (string.IsNullOrEmpty(httpVerb))
            {
                throw new ArgumentException($"{nameof(httpVerb)} 不能为空。");
            }

            HttpVerbMappings[methodPrefix] = httpVerb.ToUpperInvariant();
        }
    }
}