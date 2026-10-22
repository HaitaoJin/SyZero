using System.Runtime.CompilerServices;

// 暴露 internal 成员给单元测试项目（仅限测试场景，例如 AppConfig.ResetCacheForTests）
[assembly: InternalsVisibleTo("SyZero.Tests")]
