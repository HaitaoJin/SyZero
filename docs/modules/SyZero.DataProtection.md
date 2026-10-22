# SyZero.DataProtection

数据保护模块：在 ASP.NET Core Data Protection 之上提供 `IFieldEncryptor` 字段级加解密抽象与 `[Encrypted]` 标记，方便对 DTO / 持久化数据中的敏感字段进行可逆加密。

## 📦 安装

```bash
dotnet add package SyZero.DataProtection
```

## ✨ 特性

- 🔐 **可逆加密**：包装 `IDataProtectionProvider`，自动处理密钥派生
- 🏷️ **`[Encrypted("purpose")]`** 特性：标记字段，便于业务侧自取/反射加密
- 🔄 **Purpose 隔离**：不同业务上下文使用不同 Purpose，密钥相互隔离
- 💾 **密钥持久化**：通过 `configure` lambda 透传 ASP.NET Core 的密钥环配置（FileSystem / Redis / Azure Key Vault 等）

## 🚀 快速开始

### 1. 注册

```csharp
builder.Services.AddSyZeroDataProtection(dp =>
{
    dp.PersistKeysToFileSystem(new DirectoryInfo("/var/keys"))
      .SetApplicationName("MyApp");
});
```

### 2. 业务侧使用

```csharp
public class TokenService
{
    private readonly IFieldEncryptor _enc;
    public TokenService(IFieldEncryptor enc) => _enc = enc;

    public string Encrypt(string token) => _enc.Protect(token, "user-token")!;
    public string Decrypt(string cipher) => _enc.Unprotect(cipher, "user-token")!;
}
```

### 3. DTO 字段标注

```csharp
public class CreateUserDto
{
    public string UserName { get; set; }

    [Encrypted("user-token")]
    public string ApiToken { get; set; }
}
```

业务侧可遍历属性自动加解密；亦可结合 `IAuditDataMasker`（SyZero.Audit）在审计日志中脱敏。

## 📜 许可证

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
