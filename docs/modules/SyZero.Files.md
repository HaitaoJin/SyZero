# SyZero.Files

文件存储抽象 `ISyFileStorage` + 本地磁盘默认实现 `LocalFileStorage`，业务侧仅依赖接口，后续可替换为 S3 / OSS / COS 等对象存储。

## 📦 安装

```bash
dotnet add package SyZero.Files
```

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroLocalFiles("/var/syzero/files");

public class UploadApp(ISyFileStorage storage)
{
    public async Task<string> SaveAsync(IFormFile file)
    {
        var path = $"uploads/{Guid.NewGuid():N}/{file.FileName}";
        await using var s = file.OpenReadStream();
        return await storage.SaveAsync(path, s);
    }
}
```

## 🧩 安全

`LocalFileStorage` 内置目录穿越防御：所有相对路径解析后必须仍位于存储根之下，否则抛出 `InvalidOperationException`。

> 提示：生产环境建议把 `RootPath` 指向单独的数据卷或对象存储挂载点。
