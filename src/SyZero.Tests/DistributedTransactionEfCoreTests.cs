using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyZero.DistributedTransaction;
using SyZero.DistributedTransaction.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace SyZero.Tests;

public class DistributedTransactionEfCoreTests
{
    [Fact]
    public async Task EfCoreOutboxStore_RoundTrip_Works()
    {
        // 用持久打开的内存 SQLite 连接，避免连接关闭时丢失 schema。
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSyZeroOutboxEfCore(opt => opt.UseSqlite(connection));
        await using var sp = services.BuildServiceProvider();

        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        await using (var scope = sp.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            await store.EnqueueAsync(new OutboxMessage
            {
                EventName = "TestEvent",
                PayloadJson = "{\"hello\":1}"
            });

            var pending = await store.FetchPendingAsync(10);
            Assert.Single(pending);
            Assert.Equal("TestEvent", pending[0].EventName);

            await store.MarkPublishedAsync(pending[0].Id);
            var afterPublish = await store.FetchPendingAsync(10);
            Assert.Empty(afterPublish);
        }
    }
}
