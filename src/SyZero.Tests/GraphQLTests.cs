using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Linq;
using System.IO.Pipelines;
using System.Buffers;
using System.Text;
using HotChocolate.Transport.Formatters;
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
        var provider = sp.GetRequiredService<IRequestExecutorProvider>();
        var executor = await provider.GetExecutorAsync(provider.SchemaNames.First());
        var result = await executor.ExecuteAsync("{ ping }");
        var pipe = new Pipe();
        await JsonResultFormatter.Default.FormatAsync(result, pipe.Writer, ExecutionResultFormatFlags.None);
        await pipe.Writer.CompleteAsync();
        var read = await pipe.Reader.ReadAsync();
        var json = Encoding.UTF8.GetString(read.Buffer.ToArray());
        pipe.Reader.AdvanceTo(read.Buffer.End);
        await pipe.Reader.CompleteAsync();
        Assert.Contains("pong", json, System.StringComparison.OrdinalIgnoreCase);
    }
}
