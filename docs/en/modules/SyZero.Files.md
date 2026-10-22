# SyZero.Files

`ISyFileStorage` abstraction + `LocalFileStorage` default implementation. Business code depends only on the interface, leaving room for S3 / OSS / COS adapters later.

## 📦 Install

```bash
dotnet add package SyZero.Files
```

## 🚀 Quick Start

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

`LocalFileStorage` rejects path-traversal: every resolved path must remain under the configured root.
