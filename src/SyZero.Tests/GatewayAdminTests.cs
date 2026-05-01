using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SyZero.Gateway;
using SyZero.Gateway.Admin.Auditing;
using SyZero.Gateway.Admin.Auth;
using SyZero.Gateway.Admin.Controllers;
using SyZero.Gateway.Admin.Services;
using Xunit;

namespace SyZero.Tests;

public sealed class GatewayAdminTests : IDisposable
{
    private readonly string _root;

    public GatewayAdminTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "syzero-gw-admin-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* 测试目录清理失败忽略 */ }
    }

    // ---------- FileGatewayConfigurationStore ----------

    [Fact]
    public async Task FileStore_ReadAsync_ReturnsEmpty_WhenNoFile()
    {
        var (store, _) = CreateStore();
        var dto = await store.ReadAsync();
        Assert.NotNull(dto);
        Assert.Empty(dto.Routes);
        Assert.Empty(dto.SwaggerEndPoints);
    }

    [Fact]
    public async Task FileStore_WriteAsync_CreatesBackup_AndAppliesConfig()
    {
        var (store, applier) = CreateStore();

        // 第一次写入：不会备份（无现有文件），但会触发 Apply。
        var first = new GatewayConfiguration { Routes = { Route("/a") } };
        await store.WriteAsync(first);
        Assert.True(File.Exists(Path.Combine(_root, "configuration.json")));
        Assert.Equal(1, applier.AppliedCount);
        Assert.Empty(Directory.GetFiles(_root, "configuration.backup.*.json"));

        // 第二次写入：会先备份再写入。
        var second = new GatewayConfiguration { Routes = { Route("/b") } };
        await store.WriteAsync(second);
        var backups = Directory.GetFiles(_root, "configuration.backup.*.json");
        Assert.Single(backups);
        Assert.Equal(2, applier.AppliedCount);

        // ReadAsync 应读出最新内容。
        var read = await store.ReadAsync();
        Assert.Single(read.Routes);
        Assert.Equal("/b", read.Routes[0].UpstreamPathTemplate);
    }

    [Fact]
    public async Task FileStore_ListReadRestoreDelete_BackupLifecycle_Works()
    {
        var (store, applier) = CreateStore();

        await store.WriteAsync(new GatewayConfiguration { Routes = { Route("/v1") } });
        // 等待 1s 确保备份文件名（基于 yyyyMMddHHmmss）唯一。
        await Task.Delay(1100);
        await store.WriteAsync(new GatewayConfiguration { Routes = { Route("/v2") } });

        var backups = await store.ListBackupsAsync();
        Assert.Single(backups);
        var backupId = backups[0].Id;

        var snapshot = await store.ReadBackupAsync(backupId);
        Assert.NotNull(snapshot);
        Assert.Equal("/v1", snapshot!.Routes[0].UpstreamPathTemplate);

        // 还原会再次备份当前文件，再写入 → 备份数变为 2。等待 1.1s 避免备份名时间戳冲突。
        await Task.Delay(1100);
        await store.RestoreBackupAsync(backupId);
        var afterRestore = await store.ListBackupsAsync();
        Assert.Equal(2, afterRestore.Count);
        var restored = await store.ReadAsync();
        Assert.Equal("/v1", restored.Routes[0].UpstreamPathTemplate);

        // 删除某条备份。
        await store.DeleteBackupAsync(backupId);
        var afterDelete = await store.ListBackupsAsync();
        Assert.DoesNotContain(afterDelete, b => b.Id == backupId);
    }

    [Fact]
    public async Task FileStore_CleanupBackups_RespectsMaxCount()
    {
        var (store, _) = CreateStore(maxBackupCount: 2);

        // 第一次写入不产生备份，需要至少 4 次写入才能产生 3 个备份并触发清理。
        for (var i = 0; i < 4; i++)
        {
            await store.WriteAsync(new GatewayConfiguration { Routes = { Route("/r" + i) } });
            // 备份文件名按秒精度，确保唯一。
            await Task.Delay(1100);
        }
        var backups = Directory.GetFiles(_root, "configuration.backup.*.json");
        Assert.True(backups.Length <= 2, $"应当受 MaxCount=2 约束，实际 {backups.Length}");
    }

    [Fact]
    public async Task FileStore_ReadBackupAsync_ReturnsNull_WhenIdInvalid()
    {
        var (store, _) = CreateStore();
        Assert.Null(await store.ReadBackupAsync("../oops"));
        Assert.Null(await store.ReadBackupAsync("not-exist"));
    }

    // ---------- AdminController: reorder ----------

    [Fact]
    public async Task ReorderRoutes_ValidatesPermutation_AndAppliesNewOrder()
    {
        var ctx = await CreateControllerContextAsync(new[] { "/a", "/b", "/c" });

        // 合法：反转。
        var ok = await ctx.Controller.ReorderRoutes(
            new AdminController.ReorderRequest { Order = new List<int> { 2, 1, 0 } },
            CancellationToken.None);
        Assert.IsType<NoContentResult>(ok);
        var dto = await ctx.Store.ReadAsync();
        Assert.Equal(new[] { "/c", "/b", "/a" }, dto.Routes.Select(r => r.UpstreamPathTemplate));

        // 非法：长度不一致。
        var bad1 = await ctx.Controller.ReorderRoutes(
            new AdminController.ReorderRequest { Order = new List<int> { 0, 1 } }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(bad1);

        // 非法：不是排列（重复 0）。
        var bad2 = await ctx.Controller.ReorderRoutes(
            new AdminController.ReorderRequest { Order = new List<int> { 0, 0, 1 } }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(bad2);

        // 非法：越界。
        var bad3 = await ctx.Controller.ReorderRoutes(
            new AdminController.ReorderRequest { Order = new List<int> { 0, 1, 5 } }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(bad3);
    }

    // ---------- AdminController: bulk-toggle / bulk-tags ----------

    [Fact]
    public async Task BulkToggleRoutes_OnlyChangesTargets_AndIgnoresOutOfRange()
    {
        var ctx = await CreateControllerContextAsync(new[] { "/a", "/b", "/c" });
        var result = await ctx.Controller.BulkToggleRoutes(
            new AdminController.BulkToggleRequest { Indexes = new List<int> { 0, 2, 99 }, Disabled = true },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        // 匿名对象 → 反射读取属性。
        var changed = (int)ok.Value!.GetType().GetProperty("Changed")!.GetValue(ok.Value)!;
        Assert.Equal(2, changed);

        var dto = await ctx.Store.ReadAsync();
        Assert.True(dto.Routes[0].Disabled);
        Assert.False(dto.Routes[1].Disabled);
        Assert.True(dto.Routes[2].Disabled);
    }

    [Fact]
    public async Task BulkUpdateTags_SetAddRemove_BehavesAsSpecified()
    {
        var ctx = await CreateControllerContextAsync(new[] { "/a", "/b" });

        // set
        await ctx.Controller.BulkUpdateTags(
            new AdminController.BulkTagsRequest { Indexes = new List<int> { 0, 1 }, Tags = new List<string> { "v1", "auth" }, Op = "set" },
            CancellationToken.None);
        var afterSet = await ctx.Store.ReadAsync();
        Assert.Equal(new[] { "v1", "auth" }, afterSet.Routes[0].Tags);

        // add
        await ctx.Controller.BulkUpdateTags(
            new AdminController.BulkTagsRequest { Indexes = new List<int> { 0 }, Tags = new List<string> { "beta", "AUTH" }, Op = "add" },
            CancellationToken.None);
        var afterAdd = await ctx.Store.ReadAsync();
        // AUTH 与 auth 因 OrdinalIgnoreCase 去重。
        Assert.Equal(new[] { "v1", "auth", "beta" }, afterAdd.Routes[0].Tags);

        // remove
        await ctx.Controller.BulkUpdateTags(
            new AdminController.BulkTagsRequest { Indexes = new List<int> { 0 }, Tags = new List<string> { "AUTH" }, Op = "remove" },
            CancellationToken.None);
        var afterRemove = await ctx.Store.ReadAsync();
        Assert.Equal(new[] { "v1", "beta" }, afterRemove.Routes[0].Tags);

        // op 非法 → 400
        var bad = await ctx.Controller.BulkUpdateTags(
            new AdminController.BulkTagsRequest { Indexes = new List<int> { 0 }, Tags = new List<string>(), Op = "wat" },
            CancellationToken.None);
        var problem = Assert.IsType<ObjectResult>(bad);
        Assert.Equal(400, problem.StatusCode);
    }

    // ---------- AdminController: import / export ----------

    [Fact]
    public async Task ExportImport_JsonRoundTrip_AppendMode_Works()
    {
        var ctx = await CreateControllerContextAsync(new[] { "/a", "/b" });

        // export json
        var export = await ctx.Controller.ExportRoutes("json", CancellationToken.None);
        var content = Assert.IsType<ContentResult>(export);
        Assert.Equal("application/json", content.ContentType);
        var exported = JsonSerializer.Deserialize<List<GatewayRoute>>(content.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(2, exported.Count);

        // import 同样数据 → append 模式
        ctx.SetRequestBody(content.Content!);
        var imp = await ctx.Controller.ImportRoutes("json", "append", CancellationToken.None);
        Assert.IsType<OkObjectResult>(imp);
        var dto = await ctx.Store.ReadAsync();
        Assert.Equal(4, dto.Routes.Count);
    }

    [Fact]
    public async Task ImportRoutes_YamlReplaceMode_OverwritesAll()
    {
        var ctx = await CreateControllerContextAsync(new[] { "/old1", "/old2", "/old3" });

        const string yaml = @"
- upstreamPathTemplate: /new
  upstreamHttpMethod:
    - GET
  downstreamScheme: http
  downstreamPathTemplate: /api/new
  serviceName: NewSvc
";
        ctx.SetRequestBody(yaml);
        var imp = await ctx.Controller.ImportRoutes("yaml", "replace", CancellationToken.None);
        Assert.IsType<OkObjectResult>(imp);

        var dto = await ctx.Store.ReadAsync();
        Assert.Single(dto.Routes);
        Assert.Equal("/new", dto.Routes[0].UpstreamPathTemplate);
        Assert.Equal("NewSvc", dto.Routes[0].ServiceName);
    }

    [Fact]
    public async Task ImportRoutes_AutoDetectsFormat_ByFirstChar()
    {
        var ctx = await CreateControllerContextAsync(Array.Empty<string>());
        // 不指定 format，body 以 [ 开头 → 走 json 分支
        ctx.SetRequestBody("[{\"upstreamPathTemplate\":\"/x\",\"downstreamScheme\":\"http\",\"downstreamPathTemplate\":\"/y\"}]");
        var imp = await ctx.Controller.ImportRoutes(format: null, mode: "replace", CancellationToken.None);
        Assert.IsType<OkObjectResult>(imp);
        var dto = await ctx.Store.ReadAsync();
        Assert.Single(dto.Routes);
        Assert.Equal("/x", dto.Routes[0].UpstreamPathTemplate);
    }

    // ---------- AdminController: SwaggerEndPoints import / export ----------

    [Fact]
    public async Task ExportImport_SwaggerEndPoints_JsonRoundTrip_AppendMode_Works()
    {
        var ctx = await CreateControllerContextWithSwaggerAsync(new[] { "k1", "k2" });

        // export json
        var export = await ctx.Controller.ExportSwaggerEndPoints("json", CancellationToken.None);
        var content = Assert.IsType<ContentResult>(export);
        Assert.Equal("application/json", content.ContentType);
        var exported = JsonSerializer.Deserialize<List<GatewaySwaggerEndPoint>>(content.Content!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(2, exported.Count);

        // import 同样数据 → append 模式应当因 Key 冲突而 400
        ctx.SetRequestBody(content.Content!);
        var imp = await ctx.Controller.ImportSwaggerEndPoints("json", "append", CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(imp);
    }

    [Fact]
    public async Task ImportSwaggerEndPoints_MergeMode_OverridesByKey()
    {
        var ctx = await CreateControllerContextWithSwaggerAsync(new[] { "k1", "k2" });

        // 准备 incoming：覆盖 k1，新增 k3
        const string json = "[{\"key\":\"k1\",\"config\":[{\"name\":\"new\",\"version\":\"v2\",\"service\":{\"name\":\"S\",\"path\":\"/p\"}}]},{\"key\":\"k3\",\"config\":[]}]";
        ctx.SetRequestBody(json);
        var imp = await ctx.Controller.ImportSwaggerEndPoints("json", "merge", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(imp);
        var added = (int)ok.Value!.GetType().GetProperty("Added")!.GetValue(ok.Value)!;
        var replaced = (int)ok.Value!.GetType().GetProperty("Replaced")!.GetValue(ok.Value)!;
        Assert.Equal(1, added);
        Assert.Equal(1, replaced);

        var dto = await ctx.Store.ReadAsync();
        Assert.Equal(3, dto.SwaggerEndPoints.Count);
        var k1 = dto.SwaggerEndPoints.Single(e => e.Key == "k1");
        Assert.Single(k1.Config);
        Assert.Equal("new", k1.Config[0].Name);
        Assert.Equal("v2", k1.Config[0].Version);
    }

    [Fact]
    public async Task ImportSwaggerEndPoints_ReplaceMode_OverwritesAll()
    {
        var ctx = await CreateControllerContextWithSwaggerAsync(new[] { "old1", "old2", "old3" });
        const string yaml = @"
- key: only
  config:
    - name: api
      version: v1
      service:
        name: Svc
        path: /swagger/v1/swagger.json
";
        ctx.SetRequestBody(yaml);
        var imp = await ctx.Controller.ImportSwaggerEndPoints("yaml", "replace", CancellationToken.None);
        Assert.IsType<OkObjectResult>(imp);
        var dto = await ctx.Store.ReadAsync();
        Assert.Single(dto.SwaggerEndPoints);
        Assert.Equal("only", dto.SwaggerEndPoints[0].Key);
    }

    [Fact]
    public async Task ImportSwaggerEndPoints_RejectsItemWithoutKey()
    {
        var ctx = await CreateControllerContextWithSwaggerAsync(Array.Empty<string>());
        ctx.SetRequestBody("[{\"key\":\"\",\"config\":[]}]");
        var imp = await ctx.Controller.ImportSwaggerEndPoints("json", "replace", CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(imp);
    }

    // ---------- mapper：Disabled 跳过 ----------    [Fact]
    public void OcelotMapper_SkipsDisabledRoutes()
    {
        var dto = new GatewayConfiguration
        {
            Routes =
            {
                Route("/keep"),
                new GatewayRoute { UpstreamPathTemplate = "/skip", DownstreamScheme = "http", DownstreamPathTemplate = "/skip", Disabled = true },
            }
        };
        var fileConfig = InvokeOcelotMapper(dto);
        Assert.NotNull(fileConfig);
        // FileConfiguration.Routes 的元素是 Ocelot 内部类型，使用反射数一下数量。
        var routes = fileConfig!.GetType().GetProperty("Routes")!.GetValue(fileConfig) as System.Collections.IEnumerable;
        Assert.NotNull(routes);
        var count = 0; foreach (var _ in routes!) count++;
        Assert.Equal(1, count);
    }

    [Fact]
    public void YarpMapper_SkipsDisabledRoutes()
    {
        var dto = new GatewayConfiguration
        {
            Routes =
            {
                Route("/keep"),
                new GatewayRoute { UpstreamPathTemplate = "/skip", DownstreamScheme = "http", DownstreamPathTemplate = "/skip", Disabled = true },
            }
        };
        var (routes, _) = InvokeYarpMapper(dto);
        Assert.Single(routes);
    }

    // ---------- helpers ----------

    private static GatewayRoute Route(string upstream)
        => new GatewayRoute
        {
            UpstreamPathTemplate = upstream,
            DownstreamScheme = "http",
            DownstreamPathTemplate = upstream,
            UpstreamHttpMethod = new List<string> { "GET" }
        };

    private (FileGatewayConfigurationStore Store, CountingApplier Applier) CreateStore(int? maxBackupCount = null)
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootPath).Returns(_root);

        var dict = new Dictionary<string, string?>();
        if (maxBackupCount is int max) dict["Gateway:Admin:Backup:MaxCount"] = max.ToString();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var applier = new CountingApplier();
        var store = new FileGatewayConfigurationStore(envMock.Object, configuration, applier, NullLogger<FileGatewayConfigurationStore>.Instance);
        return (store, applier);
    }

    private async Task<ControllerHarness> CreateControllerContextAsync(IReadOnlyList<string> upstreams)
    {
        var (store, _) = CreateStore();
        if (upstreams.Count > 0)
        {
            await store.WriteAsync(new GatewayConfiguration { Routes = upstreams.Select(Route).ToList() });
        }
        var audit = new NoopAudit();
        var bus = new NoopBus();
        var controller = new AdminController(store, audit, bus, services: new EmptyServiceProvider());
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return new ControllerHarness(controller, store, httpContext);
    }

    private async Task<ControllerHarness> CreateControllerContextWithSwaggerAsync(IReadOnlyList<string> keys)
    {
        var (store, _) = CreateStore();
        if (keys.Count > 0)
        {
            var dto = new GatewayConfiguration
            {
                SwaggerEndPoints = keys.Select(k => new GatewaySwaggerEndPoint
                {
                    Key = k,
                    Config = new List<GatewaySwaggerEndPointConfig>
                    {
                        new GatewaySwaggerEndPointConfig
                        {
                            Name = k + "-api",
                            Version = "v1",
                            Service = new GatewaySwaggerService { Name = k + "-svc", Path = "/swagger/v1/swagger.json" },
                        },
                    },
                }).ToList(),
            };
            await store.WriteAsync(dto);
        }
        var audit = new NoopAudit();
        var bus = new NoopBus();
        var controller = new AdminController(store, audit, bus, services: new EmptyServiceProvider());
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return new ControllerHarness(controller, store, httpContext);
    }

    private sealed class ControllerHarness
    {
        public AdminController Controller { get; }
        public IGatewayConfigurationStore Store { get; }
        public DefaultHttpContext HttpContext { get; }
        public ControllerHarness(AdminController controller, IGatewayConfigurationStore store, DefaultHttpContext httpContext)
        { Controller = controller; Store = store; HttpContext = httpContext; }

        public void SetRequestBody(string text)
        {
            HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(text));
        }
    }

    private sealed class CountingApplier : IGatewayConfigurationApplier
    {
        public int AppliedCount { get; private set; }
        public Task ApplyAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
        { AppliedCount++; return Task.CompletedTask; }
    }

    private sealed class NoopAudit : IGatewayAdminAuditLog
    {
        public bool Enabled => false;
        public Task AppendAsync(GatewayAdminAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<GatewayAdminAuditEntry>> ReadRecentAsync(int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GatewayAdminAuditEntry>>(Array.Empty<GatewayAdminAuditEntry>());
    }

    private sealed class NoopBus : SyZero.Gateway.Admin.Events.IGatewayAdminEventBus
    {
        public int SubscriberCount => 0;
        public void Publish(SyZero.Gateway.Admin.Events.GatewayAdminEvent evt) { }
        public (Guid SubscriptionId, System.Threading.Channels.ChannelReader<SyZero.Gateway.Admin.Events.GatewayAdminEvent> Reader) Subscribe()
            => throw new NotSupportedException();
        public void Unsubscribe(Guid subscriptionId) { }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    // ---- mapper 反射调用：mapper 是 internal，所以反射调用 ----

    private static object? InvokeOcelotMapper(GatewayConfiguration dto)
    {
        var asm = typeof(SyZero.Gateway.Ocelot.SyZeroOcelotOptions).Assembly;
        var t = asm.GetType("SyZero.Gateway.Ocelot.OcelotConfigurationApplier", throwOnError: true)!;
        var m = t.GetMethod("MapToFileConfiguration", BindingFlags.NonPublic | BindingFlags.Static)!;
        return m.Invoke(null, new object?[] { dto });
    }

    private static (System.Collections.IEnumerable Routes, System.Collections.IEnumerable Clusters) InvokeYarpMapper(GatewayConfiguration dto)
    {
        var asm = typeof(SyZero.Gateway.YARP.SyZeroYarpOptions).Assembly;
        var t = asm.GetType("SyZero.Gateway.YARP.YarpConfigMapper", throwOnError: true)!;
        var m = t.GetMethod("Map", BindingFlags.Public | BindingFlags.Static)!;
        var result = m.Invoke(null, new object?[] { dto })!;
        // ValueTuple<IReadOnlyList<RouteConfig>, IReadOnlyList<ClusterConfig>>
        var rt = result.GetType();
        var routes = (System.Collections.IEnumerable)rt.GetField("Item1")!.GetValue(result)!;
        var clusters = (System.Collections.IEnumerable)rt.GetField("Item2")!.GetValue(result)!;
        return (routes, clusters);
    }

    // ---------- IpAclMatcher (IP 黑白名单) ----------

    [Theory]
    [InlineData("1.2.3.4", "1.2.3.4", true)]
    [InlineData("1.2.3.4", "1.2.3.5", false)]
    [InlineData("10.0.0.0/8", "10.5.6.7", true)]
    [InlineData("10.0.0.0/8", "11.0.0.1", false)]
    [InlineData("192.168.1.0/24", "192.168.1.250", true)]
    [InlineData("192.168.1.0/24", "192.168.2.1", false)]
    [InlineData("::1", "::1", true)]
    [InlineData("2001:db8::/32", "2001:db8:abcd::1", true)]
    [InlineData("2001:db8::/32", "2001:dead::1", false)]
    public void IpAclMatcher_SingleEntry_Matches(string entry, string client, bool expected)
    {
        var matcher = new IpAclMatcher(new[] { entry });
        var ip = System.Net.IPAddress.Parse(client);
        Assert.Equal(expected, matcher.Matches(ip));
    }

    [Fact]
    public void IpAclMatcher_EmptyOrInvalid_Treated_AsEmpty()
    {
        var matcher = new IpAclMatcher(new[] { "", "  ", "not-an-ip", "999.999.999.999" });
        Assert.True(matcher.IsEmpty);
        Assert.False(matcher.Matches(System.Net.IPAddress.Parse("1.2.3.4")));
    }

    [Fact]
    public void IpAclMatcher_MultipleEntries_AnyMatchWins()
    {
        var matcher = new IpAclMatcher(new[] { "10.0.0.0/8", "192.168.0.0/16", "127.0.0.1" });
        Assert.False(matcher.IsEmpty);
        Assert.True(matcher.Matches(System.Net.IPAddress.Parse("10.1.2.3")));
        Assert.True(matcher.Matches(System.Net.IPAddress.Parse("192.168.5.5")));
        Assert.True(matcher.Matches(System.Net.IPAddress.Parse("127.0.0.1")));
        Assert.False(matcher.Matches(System.Net.IPAddress.Parse("8.8.8.8")));
    }

    [Fact]
    public void IpAclMatcher_IPv4MappedToIPv6_NormalizesToIPv4()
    {
        var matcher = new IpAclMatcher(new[] { "10.0.0.0/8" });
        // ::ffff:10.1.2.3 应当被识别为 10.1.2.3
        var mapped = System.Net.IPAddress.Parse("10.1.2.3").MapToIPv6();
        Assert.True(matcher.Matches(mapped));
    }

    // ---------- AdminBasicAuthMiddleware: TryParseBasic / TryParseToken / FixedTimeEquals ----------

    [Fact]
    public void TryParseBasic_ValidHeader_ReturnsCredentials()
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:p@ss:word"));
        var ok = AdminBasicAuthMiddleware.TryParseBasic("Basic " + token, out var u, out var p);
        Assert.True(ok);
        Assert.Equal("alice", u);
        // 密码中带冒号：仅按第一个冒号分割。
        Assert.Equal("p@ss:word", p);
    }

    [Theory]
    [InlineData("Bearer abc")]            // 非 Basic
    [InlineData("Basic")]                 // 缺 token
    [InlineData("Basic ###not-base64###")] // 非 base64
    public void TryParseBasic_Invalid_ReturnsFalse(string header)
    {
        var ok = AdminBasicAuthMiddleware.TryParseBasic(header, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseToken_NoColon_ReturnsFalse()
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("nocolon"));
        Assert.False(AdminBasicAuthMiddleware.TryParseToken(token, out _, out _));
    }

    [Theory]
    [InlineData("admin", "admin", true)]
    [InlineData("admin", "ADMIN", false)]
    [InlineData("admin", "admin1", false)]
    [InlineData("", "", true)]
    public void FixedTimeEquals_Behaves_Like_Equals(string a, string b, bool expected)
    {
        Assert.Equal(expected, AdminBasicAuthMiddleware.FixedTimeEquals(a, b));
    }

    // ---------- FailedLoginTracker ----------

    [Fact]
    public void FailedLoginTracker_AccumulatesUntilLockout_ThenReports()
    {
        var opts = new TestOptionsMonitor<GatewayAdminRateLimitOptions>(new GatewayAdminRateLimitOptions
        {
            MaxFailures = 3,
            LockoutMinutes = 5,
            WindowMinutes = 15
        });
        var tracker = new FailedLoginTracker(opts);

        Assert.Equal(0, tracker.GetFailureCount("ip-1"));
        Assert.Equal(0, tracker.GetLockoutSeconds("ip-1"));

        Assert.Equal(1, tracker.RecordFailure("ip-1"));
        Assert.Equal(2, tracker.RecordFailure("ip-1"));
        Assert.Equal(0, tracker.GetLockoutSeconds("ip-1")); // 还未锁定
        Assert.Equal(3, tracker.RecordFailure("ip-1"));     // 触发锁定
        Assert.True(tracker.GetLockoutSeconds("ip-1") > 0);

        // 不同 key 不互相影响。
        Assert.Equal(0, tracker.GetFailureCount("ip-2"));
    }

    [Fact]
    public void FailedLoginTracker_Reset_ClearsCount()
    {
        var opts = new TestOptionsMonitor<GatewayAdminRateLimitOptions>(new GatewayAdminRateLimitOptions
        {
            MaxFailures = 5
        });
        var tracker = new FailedLoginTracker(opts);
        tracker.RecordFailure("ip-x");
        tracker.RecordFailure("ip-x");
        Assert.Equal(2, tracker.GetFailureCount("ip-x"));
        tracker.Reset("ip-x");
        Assert.Equal(0, tracker.GetFailureCount("ip-x"));
        Assert.Equal(0, tracker.GetLockoutSeconds("ip-x"));
    }

    [Fact]
    public void FailedLoginTracker_NullOrEmptyKey_DoesNotThrow()
    {
        var opts = new TestOptionsMonitor<GatewayAdminRateLimitOptions>(new GatewayAdminRateLimitOptions());
        var tracker = new FailedLoginTracker(opts);
        Assert.Equal(0, tracker.GetFailureCount(""));
        Assert.Equal(0, tracker.GetLockoutSeconds(""));
        tracker.Reset(""); // 不抛异常
        Assert.Equal(1, tracker.RecordFailure("")); // 内部归一化为 "unknown"
    }

    // ---------- CachedFailedLoginTracker ----------

    [Fact]
    public void CachedFailedLoginTracker_Counts_AcrossCalls()
    {
        var cache = new InMemoryCache();
        var opts = new TestOptionsMonitor<GatewayAdminRateLimitOptions>(new GatewayAdminRateLimitOptions
        {
            MaxFailures = 3,
            LockoutMinutes = 5,
            WindowMinutes = 15
        });
        var tracker = new CachedFailedLoginTracker(cache, opts);

        Assert.Equal(1, tracker.RecordFailure("k"));
        Assert.Equal(2, tracker.RecordFailure("k"));
        Assert.Equal(2, tracker.GetFailureCount("k"));
        Assert.Equal(0, tracker.GetLockoutSeconds("k"));

        Assert.Equal(3, tracker.RecordFailure("k"));
        Assert.True(tracker.GetLockoutSeconds("k") > 0);

        tracker.Reset("k");
        Assert.Equal(0, tracker.GetFailureCount("k"));
    }

    [Fact]
    public void CachedFailedLoginTracker_Throws_OnNullArguments()
    {
        var opts = new TestOptionsMonitor<GatewayAdminRateLimitOptions>(new GatewayAdminRateLimitOptions());
        Assert.Throws<ArgumentNullException>(() => new CachedFailedLoginTracker(null!, opts));
        Assert.Throws<ArgumentNullException>(() => new CachedFailedLoginTracker(new InMemoryCache(), null!));
    }

    // ---------- MemoryCaptchaStore ----------

    [Fact]
    public void MemoryCaptchaStore_Issue_ReturnsIdAndQuestion()
    {
        var store = new MemoryCaptchaStore();
        var c = store.Issue();
        Assert.False(string.IsNullOrEmpty(c.Id));
        Assert.Contains("=", c.Question);
    }

    [Fact]
    public void MemoryCaptchaStore_TryConsume_Correct_ReturnsTrue_AndIsSingleUse()
    {
        var store = new MemoryCaptchaStore();
        var c = store.Issue();
        // 反向解析挑战题目，得到答案。
        var answer = EvaluateChallenge(c.Question);
        Assert.True(store.TryConsume(c.Id, answer.ToString()));
        // 二次消费失败。
        Assert.False(store.TryConsume(c.Id, answer.ToString()));
    }

    [Fact]
    public void MemoryCaptchaStore_TryConsume_Wrong_ReturnsFalse_AndConsumesEntry()
    {
        var store = new MemoryCaptchaStore();
        var c = store.Issue();
        Assert.False(store.TryConsume(c.Id, "999999"));
        // 错答也会消费掉条目（实现使用 TryRemove），再次提交正确答案也应失败。
        var answer = EvaluateChallenge(c.Question);
        Assert.False(store.TryConsume(c.Id, answer.ToString()));
    }

    [Theory]
    [InlineData(null, "1")]
    [InlineData("", "1")]
    [InlineData("id", null)]
    [InlineData("id", "")]
    public void MemoryCaptchaStore_TryConsume_NullOrEmpty_ReturnsFalse(string? id, string? answer)
    {
        var store = new MemoryCaptchaStore();
        Assert.False(store.TryConsume(id!, answer!));
    }

    private static int EvaluateChallenge(string question)
    {
        // 形如 "7 + 4 = ?" 或 "8 - 3 = ?"
        var parts = question.Replace("= ?", string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var a = int.Parse(parts[0]);
        var b = int.Parse(parts[2]);
        return parts[1] == "+" ? a + b : a - b;
    }

    // ---------- 测试辅助 ----------

    private sealed class TestOptionsMonitor<T> : Microsoft.Extensions.Options.IOptionsMonitor<T> where T : class
    {
        private T _value;
        public TestOptionsMonitor(T value) { _value = value; }
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable OnChange(Action<T, string?> listener) => new NoopDisposable();
        private sealed class NoopDisposable : IDisposable { public void Dispose() { } }
    }

    private sealed class InMemoryCache : SyZero.Cache.ICache
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object?> _data = new();
        public bool Exist(string key) => _data.ContainsKey(key);
        public string[] GetKeys(string pattern) => _data.Keys.ToArray();
        public T? Get<T>(string key) => _data.TryGetValue(key, out var v) && v is T t ? t : default;
        public void Remove(string key) => _data.TryRemove(key, out _);
        public Task RemoveAsync(string key) { _data.TryRemove(key, out _); return Task.CompletedTask; }
        public void Refresh(string key) { /* noop */ }
        public Task RefreshAsync(string key) => Task.CompletedTask;
        public void Set<T>(string key, T value, int exprireTime = 86400) => _data[key] = value;
        public Task SetAsync<T>(string key, T value, int exprireTime = 86400) { _data[key] = value; return Task.CompletedTask; }
    }
}
