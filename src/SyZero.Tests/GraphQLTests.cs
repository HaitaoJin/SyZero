using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace SyZero.Tests;

public class GraphQLTests
{
    [Fact]
    public async Task DefaultPingQuery_Works()
    {
        var services = new ServiceCollection();
        services.AddSyZeroGraphQL();
        var sp = services.BuildServiceProvider();
        var executor = await sp.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
        var result = await executor.ExecuteAsync("{ ping }");
        var formatter = new JsonResultFormatter();
        var json = formatter.Format((IOperationResult)result);
        Assert.Contains("pong", json);
    }
}
