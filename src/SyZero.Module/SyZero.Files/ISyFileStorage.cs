using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Files;

/// <summary>
/// SyZero 文件存储抽象。统一上传、下载、删除、判断存在的接口，
/// 默认提供本地磁盘实现 <see cref="LocalFileStorage"/>，后续可对接 S3/OSS/COS 等。
/// </summary>
public interface ISyFileStorage
{
    /// <summary>
    /// 保存一个流到指定路径（相对存储根），覆盖同名文件。
    /// </summary>
    /// <returns>保存后用于读取的相对路径（通常等于入参 path）。</returns>
    Task<string> SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);

    /// <summary>打开指定路径的只读流。</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
