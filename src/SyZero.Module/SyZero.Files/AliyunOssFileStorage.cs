using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aliyun.OSS;

namespace SyZero.Files;

public sealed class AliyunOssOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}

/// <summary>统一文件抽象的阿里云 OSS 生产实现。</summary>
public sealed class AliyunOssFileStorage : ISyFileStorage, IFileUrlProvider
{
    private readonly IOss _client;
    private readonly string _bucket;
    public AliyunOssFileStorage(AliyunOssOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.BucketName)) throw new ArgumentException("BucketName 不能为空。", nameof(options));
        _client = new OssClient(options.Endpoint, options.AccessKeyId, options.AccessKeySecret);
        _bucket = options.BucketName;
    }

    public Task<string> SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _client.PutObject(_bucket, Normalize(path), content);
        return Task.FromResult(Normalize(path));
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Stream>(_client.GetObject(_bucket, Normalize(path)).Content);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(_client.DoesObjectExist(_bucket, Normalize(path)));

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _client.DeleteObject(_bucket, Normalize(path));
        return Task.CompletedTask;
    }

    public Task<FileStorageObject?> GetMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        var key = Normalize(path);
        if (!_client.DoesObjectExist(_bucket, key)) return Task.FromResult<FileStorageObject?>(null);
        var metadata = _client.GetObjectMetadata(_bucket, key);
        return Task.FromResult<FileStorageObject?>(new(key, metadata.ContentLength, metadata.ContentType, metadata.LastModified));
    }

    public async IAsyncEnumerable<FileStorageObject> ListAsync(string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? marker = null;
        do
        {
            var result = _client.ListObjects(new ListObjectsRequest(_bucket) { Prefix = prefix, Marker = marker });
            foreach (var item in result.ObjectSummaries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(item.Key, item.Size, null, item.LastModified);
                await Task.Yield();
            }
            marker = result.IsTruncated ? result.NextMarker : null;
        } while (marker is not null);
    }

    public Task<Uri> CreateDownloadUrlAsync(string path, TimeSpan lifetime, CancellationToken cancellationToken = default)
        => Task.FromResult(_client.GeneratePresignedUri(_bucket, Normalize(path), DateTime.Now.Add(lifetime)));

    private static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Path.IsPathRooted(path) || path.Split('/', '\\').Contains("..")) throw new ArgumentException("必须使用安全的相对对象键。", nameof(path));
        return path.Replace('\\', '/').TrimStart('/');
    }
}
