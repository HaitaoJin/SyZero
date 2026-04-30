---
description: SyZero NuGet 发布相关文件（Directory.Build.props / 版本说明）的协同变更约束
applyTo: "Directory.Build.props,Directory.Packages.props,docs/release-notes.md,docs/en/release-notes.md,nuget/**"
---

# 发布流程约束

适用于会**影响 NuGet 包内容或版本**的文件。完整发布工作流见 [/release-nuget](../prompts/release-nuget.prompt.md)。

## 强约束

- 修改 [Directory.Build.props](../../Directory.Build.props) 的 `<Version>` 会**统一改变所有 SyZero.* 包版本**——这是设计意图，禁止为单包覆盖版本。
- 任何 `<Version>` 变更必须**同步**追加 [docs/release-notes.md](../../docs/release-notes.md) 与 [docs/en/release-notes.md](../../docs/en/release-notes.md) 条目（中英文版本号一致）。
- 不要在 [Directory.Packages.props](../../Directory.Packages.props) 中删除已被项目引用的 `<PackageVersion>` —— 先在 csproj 中移除引用，再清理版本登记。
- 升级第三方依赖大版本时，须在版本说明中注明可能的破坏性变化（特别是 `Microsoft.EntityFrameworkCore` / `SqlSugarCore` / `Ocelot` / `RabbitMQ.Client`）。

## 安全约束

- **不要**自动执行 [nuget/批量生成nuget.bat](../../nuget/批量生成nuget.bat) 或 [nuget/推送nuget.bat](../../nuget/推送nuget.bat)，必须由用户显式触发 `/release-nuget` 工作流。
- **不要**把 NuGet API Key、签名证书等机密写入仓库或脚本。
- **不要**删除 `nuget/` 下已发布版本的 `*.nupkg` / `*.snupkg`，它们是发布快照，可作为回滚证据。
- 已推送至 nuget.org 的版本号**不可复用**——若需撤回，使用 unlist 而非删除，并发新版本号。

## 不要做

- 不要为了"测试"在仓库主分支上调高 `<Version>` 后再调低——保持单调递增。
- 不要绕过中央版本管理（在 csproj 直接写 `Version="..."`）；唯一例外是测试项目的 `VersionOverride`，且需有明确理由。
- 不要在版本说明里复制 commit 列表，应总结面向使用者的**行为变化**（新增/破坏/修复/升级）。
