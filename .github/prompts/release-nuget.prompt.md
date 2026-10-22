---
description: SyZero 全量 NuGet 包发布检查清单（升版本 → 打包 → 推送）
mode: agent
---

# 发布 SyZero NuGet 包

⚠️ **此流程会改变所有 `SyZero.*` 包的版本并把产物推送到 nuget.org**。在用户**显式确认**新版本号之前，不要执行任何写操作或脚本。

## 步骤

1. **确认目标版本号**：询问用户新的语义化版本（如 `1.1.11`）。当前版本见 [Directory.Build.props](../../Directory.Build.props) 中的 `<Version>`。
2. **预检**：
   - `dotnet build SyZero.slnx -c Release` 必须通过。
   - `dotnet test src/SyZero.Tests/SyZero.Tests.csproj` 必须通过（或与用户确认可跳过的环境性失败）。
   - 检查 [docs/release-notes.md](../../docs/release-notes.md) 是否已为新版本添加条目，未添加则补上。
3. **升版本**：编辑 [Directory.Build.props](../../Directory.Build.props)，将 `<Version>1.1.10</Version>` 改为新版本——这会**统一**改变所有包版本（这是预期行为）。
4. **清理旧产物**：运行 [Clear.bat](../../Clear.bat) 或手动删除 `nuget/` 中旧版本的 `*.nupkg` / `*.snupkg`（保留即可，但建议清理避免误推）。
5. **打包**：运行 [nuget/批量生成nuget.bat](../../nuget/批量生成nuget.bat)。验证 `nuget/` 下出现所有 `SyZero.*.<新版本>.nupkg` 与 `.snupkg`。
6. **推送**（**需要用户最终确认**）：运行 [nuget/推送nuget.bat](../../nuget/推送nuget.bat)。需要 NuGet API Key——询问用户而**不要**写入仓库。
7. **打 tag / 提交**（询问后执行）：
   - 提交 `Directory.Build.props` 与 `docs/release-notes.md` 的变更（中文 commit message）。
   - 创建 git tag `v<新版本>` 并推送。

## 安全约束

- **不要**自作主张推送到 nuget.org —— `推送nuget.bat` 必须在用户明确同意后执行。
- **不要**把 NuGet API Key 写入任何文件或 commit。
- **不要**强推（`git push --force`）或重写已发布的 tag。

## 回滚

- 推送失败：检查 nuget.org 凭据；删除 `nuget/` 下错误产物；纠正版本后重试。
- 已推送但需撤回：在 nuget.org 上 unlist（**不**删除）受影响的包版本；本仓库直接发新版本而非复用旧号。
