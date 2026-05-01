using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using SyZero.Configurations;

namespace SyZero
{
    public class AppConfig
    {
        public static IConfiguration Configuration { get; set; }
        //  public static readonly IConfiguration Configuration;

        static AppConfig()
        {
            if (Configuration == null)
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.json", true, true)
                    .AddJsonFile($"appsettings.{environment}.json", true, true)
                    .AddEnvironmentVariables()
                    .Build();
            }
        }

        public static T? GetSection<T>(string key) where T : class, new()
        {
            var obj = Configuration.GetSection(key).Get<T>();
            return obj;
        }

        public static string? GetSection(string key)
        {
            return Configuration.GetValue<string>(key);
        }

        // 使用 Lazy<T> 保证 ServerOptions / ConnectionOptions 的初始化在多线程场景下原子且只执行一次。
        private static Lazy<SyZeroServerOptions> _serverOptions =
            new Lazy<SyZeroServerOptions>(BuildServerOptions, LazyThreadSafetyMode.ExecutionAndPublication);

        private static Lazy<SyZeroConnectionOptions> _connectionOptions =
            new Lazy<SyZeroConnectionOptions>(BuildConnectionOptions, LazyThreadSafetyMode.ExecutionAndPublication);

        public static SyZeroServerOptions ServerOptions => _serverOptions.Value;

        public static SyZeroConnectionOptions ConnectionOptions => _connectionOptions.Value;

        private static SyZeroServerOptions BuildServerOptions()
        {
            SyZeroServerOptions options = GetSection<SyZeroServerOptions>("SyZero") ?? new SyZeroServerOptions();
            if (string.IsNullOrEmpty(options.Ip))
            {
                options.Ip = ResolveLocalIPv4();
            }
            if (string.IsNullOrEmpty(options.WanIp))
            {
                options.WanIp = ResolveLocalIPv4();
            }
            return options;
        }

        private static SyZeroConnectionOptions BuildConnectionOptions()
        {
            return GetSection<SyZeroConnectionOptions>("ConnectionString") ?? new SyZeroConnectionOptions();
        }

        private static string? ResolveLocalIPv4()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .Where(p => p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || p.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred)
                    && !System.Net.IPAddress.IsLoopback(p.Address))
                .FirstOrDefault()?.Address.ToString();
        }

        /// <summary>
        /// 仅供测试场景重置缓存，避免单元测试相互污染。
        /// 受 InternalsVisibleTo 控制，不暴露给业务代码。
        /// </summary>
        /// <param name="fieldName">支持 <c>serverOptions</c> 或 <c>connectionOptions</c>（与历史命名兼容）。</param>
        internal static void ResetCacheForTests(string fieldName)
        {
            switch (fieldName)
            {
                case "serverOptions":
                case "_serverOptions":
                    _serverOptions = new Lazy<SyZeroServerOptions>(BuildServerOptions, LazyThreadSafetyMode.ExecutionAndPublication);
                    break;
                case "connectionOptions":
                case "_connectionOptions":
                    _connectionOptions = new Lazy<SyZeroConnectionOptions>(BuildConnectionOptions, LazyThreadSafetyMode.ExecutionAndPublication);
                    break;
                default:
                    throw new ArgumentException($"未知的 AppConfig 缓存字段: {fieldName}", nameof(fieldName));
            }
        }
    }
}
