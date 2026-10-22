---
description: SyZero 文档（docs/**）的写作与结构约束
applyTo: "docs/**/*.md"
---

# 文档写作约束

适用范围：[docs/](../../docs) VitePress 文档站点。

## 关键事实

- `docs/modules/SyZero.<Module>.md` **同时** 被打包为对应 NuGet 包的 `README.md`（由 [Directory.Build.props](../../Directory.Build.props) 中 `PackageReadmeFile` 规则注入）。**模块新增/重命名必须同步本目录文件**，否则 NuGet 包将缺少 README。
- 中文文档放在 `docs/`，对应英文版本放在 `docs/en/` 且**结构镜像**保持一致。
- 模块 README 必须遵循模板：[docs/modules/MODULE_TEMPLATE.md](../../docs/modules/MODULE_TEMPLATE.md)。

## 写作风格

- 默认中文，使用全角标点（中文逗号 `，`、句号 `。`、冒号 `：`、引号 `「」` 或 `""`）。
- 中英文之间保留一个空格：例如 `使用 SyZero 框架`，而不是 `使用SyZero框架`。
- 代码块语言标签必须明确：` ```csharp ` / ` ```json ` / ` ```bash ` / ` ```pwsh `。
- 章节使用 emoji 前缀与 README 风格保持一致（✨ 特性、🚀 快速开始、📦 安装、🛠️ 配置、💡 提示）。
- 链接使用相对路径，不要硬编码 `https://docs.syzero.com/...`。

## 必须遵守

- 新增模块：复制 [MODULE_TEMPLATE.md](../../docs/modules/MODULE_TEMPLATE.md) → `docs/modules/SyZero.<Module>.md`，替换全部 `{占位符}`。
- 修改框架模块的 `Add*/Use*` 公共扩展签名时，**同步**更新对应 `docs/modules/<Module>.md` 的"快速开始"代码示例。
- 在 [docs/release-notes.md](../../docs/release-notes.md) 为每次版本变更追加条目（与 [Directory.Build.props](../../Directory.Build.props) 中 `<Version>` 一致）。

## 不要做

- 不要把示例 `appsettings.json` 中的密码/连接串写成真实值，使用 `123456` / `localhost` 等明显占位符。
- 不要在文档里嵌入大段从 [README.md](../../README.md) 复制的内容——统一在模块 README 里聚焦该模块自身能力。
- 不要新增非 `MODULE_TEMPLATE` 节点结构的模块文档（保持站点导航一致）。
