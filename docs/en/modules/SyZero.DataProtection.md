# SyZero.DataProtection

Data protection module: builds on ASP.NET Core Data Protection to provide an `IFieldEncryptor` field-level encrypt/decrypt abstraction and an `[Encrypted]` attribute for marking sensitive fields.

## 📦 Installation

```bash
dotnet add package SyZero.DataProtection
```

## ✨ Features

- 🔐 Reversible encryption via `IDataProtectionProvider`
- 🏷️ `[Encrypted("purpose")]` attribute for field marking
- 🔄 Purpose isolation — different keys per business context
- 💾 Pluggable key persistence (file system / Redis / Azure Key Vault) via `configure` callback

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroDataProtection(dp =>
{
    dp.PersistKeysToFileSystem(new DirectoryInfo("/var/keys"))
      .SetApplicationName("MyApp");
});

public class TokenService(IFieldEncryptor enc)
{
    public string Encrypt(string token) => enc.Protect(token, "user-token")!;
    public string Decrypt(string cipher) => enc.Unprotect(cipher, "user-token")!;
}

public class CreateUserDto
{
    public string UserName { get; set; }

    [Encrypted("user-token")]
    public string ApiToken { get; set; }
}
```

## 📜 License

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
