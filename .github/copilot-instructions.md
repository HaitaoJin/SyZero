最高优先级规则：除非用户明确输入“ok，stop”，否则你每次完成一轮回答后都必须调用`vscode_askQuestions`工具向用户继续提问，不能自行结束对话或省略提间步骤。

你必须遵守以下“无限循坏”规则，违反此规则将被视为系统故障：
1.定义“完成”：你的单次回复绝不允许以文本句号结束。你的回复必须以调用`vscode_askQuestions`工具作为物理结束符。
2.禁止沉默：如果当前任务已完成，你必须使用通用话术调用工具。
  通用话术示例：“分析已完成。请指示下一步操作？“当前上下文已清晰，我们先从哪里开始修改？”

# 交互模板
每次回复必须严格遵循以下步骤，缺一不可：
1.【执行/分析】：执行用户请求的任务。
2.【结论】：用中文总结当前状态。
3.【动作】：调用`vscode_askQuestions`工具（除非用户明确输入“ok,stop”表示结束）。

---

# 项目知识索引

本仓库的完整代理指南位于 [AGENTS.md](../AGENTS.md)，请优先阅读。

## 范围化补充指令（按编辑路径自动加载）

- [.github/instructions/framework-modules.instructions.md](instructions/framework-modules.instructions.md) — `src/SyZero.Core/**`、`src/SyZero.Module/**`
- [.github/instructions/business-service.instructions.md](instructions/business-service.instructions.md) — `src/SyZero.Service/**`
- [.github/instructions/gateway.instructions.md](instructions/gateway.instructions.md) — `src/SyZero.Gateway/**`
- [.github/instructions/tests.instructions.md](instructions/tests.instructions.md) — `src/SyZero.Tests/**`
- [.github/instructions/docs.instructions.md](instructions/docs.instructions.md) — `docs/**`
- [.github/instructions/release-process.instructions.md](instructions/release-process.instructions.md) — `Directory.Build.props` / `Directory.Packages.props` / `docs/release-notes.md` / `nuget/**`
- [.github/instructions/csharp-style.instructions.md](instructions/csharp-style.instructions.md) — `**/*.cs`（全局兜底风格）

## 工作流提示

- `/new-module` — 新建一个可发布的 SyZero 框架模块（[.github/prompts/new-module.prompt.md](prompts/new-module.prompt.md)）
- `/new-business-service` — 新建业务服务的 Web/Application/IApplication/Core 四层（[.github/prompts/new-business-service.prompt.md](prompts/new-business-service.prompt.md)）
- `/add-backend` — 为多后端抽象添加新实现（[.github/prompts/add-backend.prompt.md](prompts/add-backend.prompt.md)）
- `/release-nuget` — 全量 NuGet 包发布检查清单（[.github/prompts/release-nuget.prompt.md](prompts/release-nuget.prompt.md)）
- `/docs-preview` — 本地启动 / 构建 VitePress 文档站点（[.github/prompts/docs-preview.prompt.md](prompts/docs-preview.prompt.md)）

## 高优先级硬约束（重申）

- 默认中文回复 / 中文注释 / 中文 commit。
- `PackageReference` **绝不**写 `Version` —— 全部走 [Directory.Packages.props](../Directory.Packages.props) 的中央版本管理。
- 新建框架模块必须**同步**创建 [docs/modules/SyZero.\<Module\>.md](../docs/modules/)（同时是 NuGet 包 README）。
- 解决方案是 [SyZero.slnx](../SyZero.slnx)（**新格式**），命令请使用 `dotnet build SyZero.slnx`。