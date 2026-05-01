# SyZero.Example3.Frontend

`SyZero.Example3` 的 React 前端（SPA 单体宿主）。

## 技术栈

- React 19 + TypeScript + Vite 6
- Tailwind CSS v4 + shadcn/ui 风格组件
- TanStack Query + React Router v7 + Zustand
- Axios（开发期通过 Vite proxy 转发到后端 `http://localhost:5004`）

## 开发

```bash
npm install
npm run dev          # 启动 Vite 开发服务器，端口 5174
```

直接访问 <http://localhost:5174>，所有 `/api/*` 请求会被代理到后端 `5004`。

如果通过 ASP.NET Core 一并启动（在 SyZero.Example3.Web 中按 F5），
SpaProxy 中间件会自动启动 `npm run dev` 并把根路径转发到前端。

## 构建

```bash
npm run build
```

产物输出到 `dist/`。`SyZero.Example3.Web.csproj` 在 `dotnet publish` 时会
自动调用 `npm run build` 并把 `dist/` 内容拷贝到 `wwwroot/`，由后端单体宿主提供。
