# Example3.SPA: React + ASP.NET Core Single-Host Sample

`SyZero.Example3.SPA` demonstrates how to bundle a **React + Vite** front-end with an **ASP.NET Core 9 + SyZero** back-end as a single host: during development, [SyZero.AspNetCore.SpaProxy](/en/modules/SyZero.AspNetCore.SpaProxy) launches Vite automatically; on publish, the front-end build output is copied into `wwwroot/` and served from the same origin.

> Source: `src/SyZero.Service/SyZero.Example3.SPA/`

## ✨ Layout

```
SyZero.Example3.SPA/
├─ SyZero.Example3.Core/              # Entities + SqlSugar repositories
├─ SyZero.Example3.IApplication/      # Application service contracts + DTOs (DynamicWebApi)
├─ SyZero.Example3.Application/       # Service implementations + AutoMapper profiles
├─ SyZero.Example3.Web/               # ASP.NET Core host (port 5004)
├─ SyZero.Example3.Frontend/          # React 19 + Vite 7 + Tailwind v4 (port 5174)
├─ app/                               # Auto-aggregated IApplication.dll output
└─ SyZero.Example3.SPA.sln            # Legacy-VS compatible solution
```

## 📦 Stack

| Layer | Tech |
| --- | --- |
| Front-end | React 19, TypeScript, Vite 6, Tailwind CSS v4, shadcn-style components, TanStack Query, React Router 7, Axios, Zustand |
| Back-end | .NET 10, SyZero, SqlSugar (default SQLite `app.db`), AutoMapper, Log4Net, native OpenAPI, Newtonsoft.Json |
| Integration | `SyZero.AspNetCore.SpaProxy` (dev-time reverse proxy), `MapFallbackToFile("/index.html")` for SPA routing |

## 🚀 Run in Development

```powershell
# 1) Install front-end deps once
cd src/SyZero.Service/SyZero.Example3.SPA/SyZero.Example3.Frontend
npm install

# 2) Start the back-end (SpaProxy auto-launches front-end on 5174)
cd ../SyZero.Example3.Web
dotnet run
```

Browse:

- `http://localhost:5004/` — single-host entry (falls back to React routes)
- `http://localhost:5004/openapi/v1.json` — OpenAPI JSON
- `http://localhost:5174/` — Vite dev server directly (alternative; `/api` is proxied to 5004)

## 🛠️ Conventions

- **Ports**: back-end 5004 / front-end dev 5174, baked into `Properties/launchSettings.json`, `vite.config.ts`, and each `appsettings.*.json`.
- **HostingStartup**: `launchSettings.json` sets `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=SyZero.AspNetCore.SpaProxy` to enable the middleware.
- **API prefix**: DynamicWebApi exposes `/api/SyZero.Example3.SPA/<Service>/<Method>`; front-end axios uses `baseURL = '/api'`.
- **App-service aggregation**: the `AppServiceCopy` MSBuild target in `SyZero.Example3.IApplication` copies its dll to `../app/` after every build (matches Example1/2).

## 📤 Publish as a Single Artifact

```powershell
cd src/SyZero.Service/SyZero.Example3.SPA/SyZero.Example3.Web
dotnet publish -c Release -o ./out
```

The `PublishRunWebpack` target runs `npm install && npm run build` and copies `Frontend/dist/` into `out/wwwroot/`. Run `dotnet out/SyZero.Example3.Web.dll` to serve API + static assets from the same origin.

## 💡 Differences vs. Example1 / Example2

| Aspect | Example1 / Example2 | Example3.SPA |
| --- | --- | --- |
| Topology | Microservices (Feign cross-calls, local ServiceManagement) | Single host (front + back same origin) |
| Service discovery | LocalServiceManagement / Consul / Nacos | Not required |
| Front-end | None | React + Vite + Tailwind + shadcn |
| Use case | Distributed service orchestration demo | Internal tools / admin console with embedded SPA |
