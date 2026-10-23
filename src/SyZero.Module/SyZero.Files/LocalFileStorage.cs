using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SyZero.Dependency;

namespace SyZero.Files;

/// <summary>
/// 基于本地磁盘的 <see cref="ISyFileStorage"/> 默认实现。所有路径相对 <see cref="RootPath"/>。
/// </summary>
public sealed class LocalFileStorage : ISyFileStorage, ISingletonDependency
{
    public string RootPath { get; }

    public LocalFileStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("rootPath is required", nameof(rootPath));
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
    }

    public async Task<string> SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        return path;
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);
        Stream s = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(s);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(Resolve(path)));

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public Task<FileStorageObject?> GetMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(Resolve(path));
        return Task.FromResult<FileStorageObject?>(file.Exists
            ? new(path.Replace('\\', '/'), file.Length, null, file.LastWriteTimeUtc)
            : null);
    }

    public async IAsyncEnumerable<FileStorageObject> ListAsync(string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var basePath = string.IsNullOrWhiteSpace(prefix) ? RootPath : Resolve(prefix);
        if (!Directory.Exists(basePath)) yield break;
        foreach (var path in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = new FileInfo(path);
            yield return new(Path.GetRelativePath(RootPath, path).Replace('\\', '/'), file.Length, null, file.LastWriteTimeUtc);
            await Task.Yield();
        }
    }

    private string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is required", nameof(path));
        if (Path.IsPathRooted(path)) throw new InvalidOperationException("path 必须是相对路径。");

        var combined = Path.GetFullPath(Path.Combine(RootPath, path));
        var relative = Path.GetRelativePath(RootPath, combined);
        if (relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("path 越过了存储根。");
        }
        return combined;
    }
}
