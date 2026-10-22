# Example3.SPA：React + ASP.NET Core 单体宿主示例

`SyZero.Example3.SPA` 演示如何把 **React + Vite** 前端与 **ASP.NET Core 9 + SyZero** 后端打包成一个单体宿主：开发期由 [SyZero.AspNetCore.SpaProxy](/modules/SyZero.AspNetCore.SpaProxy) 自动启动 Vite，发布期把前端构建产物拷贝到 `wwwroot/` 由后端同源提供。

> 源码位置：`src/SyZero.Service/SyZero.Example3.SPA/`

## ✨ 项目结构

```
SyZero.Example3.SPA/
├─ SyZero.Example3.Core/              # 实体、SqlSugar 仓储
├─ SyZero.Example3.IApplication/      # 应用服务接口 + DTO（DynamicWebApi）
├─ SyZero.Example3.Application/       # 应用服务实现 + AutoMapper Profile
├─ SyZero.Example3.Web/               # ASP.NET Core 宿主（端口 5004）
├─ SyZero.Example3.Frontend/          # React 19 + Vite 7 + Tailwind v4（端口 5174）
├─ app/                               # IApplication.dll 自动汇集目录
└─ SyZero.Example3.SPA.sln            # 旧版 VS 兼容解决方案
```

## 📦 技术栈

| 层 | 技术 |
| --- | --- |
| 前端 | React 19、TypeScript、Vite 6、Tailwind CSS v4、shadcn/ui 风格、TanStack Query、React Router 7、Axios、Zustand |
| 后端 | .NET 9、SyZero 框架、SqlSugar（默认 SQLite `app.db`）、AutoMapper、Log4Net、Swagger、Newtonsoft.Json |
| 集成 | `SyZero.AspNetCore.SpaProxy`（开发期 dev server 反代）、统一 `MapFallbackToFile("/index.html")` SPA 路由 |

## 🚀 启动开发模式

```pwsh
# 1) 安装前端依赖（首次）
cd src/SyZero.Service/SyZero.Example3.SPA/SyZero.Example3.Frontend
npm install

# 2) 启动后端（会通过 SpaProxy 自动拉起前端 5174）
cd ../SyZero.Example3.Web
dotnet run
```

浏览器访问：

- `http://localhost:5004/`：单体宿主入口（自动 fallback 到 React 路由）
- `http://localhost:5004/api/swagger`：Swagger UI
- `http://localhost:5174/`：直连 Vite Dev Server（备选，`/api` 通过代理转发到 5004）

## 🛠️ 关键约定

- **端口**：后端 5004 / 前端 dev 5174，已写入 `Properties/launchSettings.json`、`vite.config.ts` 与各 `appsettings.*.json`。
- **HostingStartup**：`launchSettings.json` 中 `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=SyZero.AspNetCore.SpaProxy` 触发 SpaProxy 中间件。
- **API 前缀**：DynamicWebApi 默认 `/api/SyZero.Example3.SPA/<Service>/<Method>`，前端 axios `baseURL = '/api'`。
- **应用服务自动汇集**：`SyZero.Example3.IApplication` 的 `AppServiceCopy` MSBuild 目标在每次 build 后会把自身 dll 复制到 `../app/`，便于宿主统一加载（与 Example1/2 一致）。

## 📤 发布为单体

```pwsh
cd src/SyZero.Service/SyZero.Example3.SPA/SyZero.Example3.Web
dotnet publish -c Release -o ./out
```

`PublishRunWebpack` 目标会自动执行 `npm install && npm run build`，并把 `Frontend/dist/` 拷贝到 `out/wwwroot/`。运行 `dotnet out/SyZero.Example3.Web.dll` 即可同源对外提供 API + 静态资源。

## 💡 与 Example1 / Example2 的区别

| 维度 | Example1 / Example2 | Example3.SPA |
| --- | --- | --- |
| 形态 | 微服务（Feign 互调、本地 ServiceManagement） | 单体宿主（前后端同源） |
| 服务发现 | 依赖 LocalServiceManagement / Consul / Nacos | 不需要 |
| 前端 | 无 | React + Vite + Tailwind + shadcn |
| 适用场景 | 演示分布式服务编排 | 演示中后台 / 控制台类内嵌 SPA |
