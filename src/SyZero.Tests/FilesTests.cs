using System.IO;
using System.Text;
using System.Threading.Tasks;
using SyZero.Files;
using Xunit;

namespace SyZero.Tests;

public class FilesTests
{
    [Fact]
    public async Task Local_storage_round_trip()
    {
        var root = Path.Combine(Path.GetTempPath(), "syzero-files-" + System.Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(root);

        var path = "sub/dir/hello.txt";
        var bytes = Encoding.UTF8.GetBytes("hello sy");
        using (var ms = new MemoryStream(bytes))
        {
            await storage.SaveAsync(path, ms);
        }

        Assert.True(await storage.ExistsAsync(path));

        await using (var read = await storage.OpenReadAsync(path))
        using (var sr = new StreamReader(read))
        {
            Assert.Equal("hello sy", await sr.ReadToEndAsync());
        }

        await storage.DeleteAsync(path);
        Assert.False(await storage.ExistsAsync(path));

        Directory.Delete(root, recursive: true);
    }
}
