---
description: 本地启动 / 构建 SyZero VitePress 文档站点（docs/）
mode: agent
---

# 文档站点（VitePress）

[docs/](../../docs) 是 VitePress 文档站点，脚本定义见 [docs/package.json](../../docs/package.json)。

## 命令（PowerShell，使用 `;` 而非 `&&`）

| 目的 | 命令 |
|---|---|
| 安装依赖（首次或 `package.json` 变更后） | `cd docs; npm install` |
| 本地开发预览（热更新，监听 `0.0.0.0:5173`） | `cd docs; npm run docs:dev` |
| 生产构建到 `docs/.vitepress/dist/` | `cd docs; npm run docs:build` |
| 预览构建产物（`0.0.0.0:4173`） | `cd docs; npm run docs:preview` |

> 长时间运行的 `dev` / `preview` 进程使用异步终端模式启动，不要 `Start-Sleep` 轮询——在用户提示完成后再检查输出。

## 工作流

1. 询问用户意图：开发预览 / 生产构建 / 仅安装依赖。
2. 检查 `docs/node_modules/` 是否存在；不存在则先 `npm install`。
3. 按需运行对应脚本；`dev`/`preview` 用异步模式，`build` 用同步等待完成。
4. 构建后简要汇报产物路径或预览 URL（`http://localhost:5173` / `http://localhost:4173`）。

## 约束

- **不要**修改 [docs/package.json](../../docs/package.json) 的脚本名（`docs:dev` / `docs:build` / `docs:preview`），它们是约定。
- **不要**提交 `docs/.vitepress/cache/` 或 `docs/.vitepress/dist/` —— 应在 `.gitignore` 中。
- 修改 `docs/modules/*.md` 后，记得：这些文件**同时是 NuGet 包 README**，详见 [docs.instructions.md](../instructions/docs.instructions.md)。
- 中英文目录结构必须镜像（`docs/` ↔ `docs/en/`）。
