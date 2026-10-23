# Example5.Frontend 网关前端示例

`src/SyZero.Service/SyZero.Example5.Frontend/` 是一个独立的 TypeScript + Vite 客户端，用于演示浏览器通过 `SyZero.Gateway` 统一访问 Example1 与 Example2。它不包含后端宿主，也不会代理 Example3；Example3 仍是前后端同宿主的单体 SPA 示例。

## 调用链

```text
Browser → Example5 Vite → SyZero.Gateway :5000 → Example1 :5002 / Example2 :5003
```

前端包含两组调用。服务状态接口验证网关转发：

```text
/example1/api/SyZero.Example1/Example1/GetServiceInfo
/example2/api/SyZero.Example2/Example2/GetServiceInfo
```

服务互调接口会继续从下游服务调用另一个服务：

```text
/example1/api/SyZero.Example1/Example1/CallExample2Service
/example2/api/SyZero.Example2/Example2/CallExample1Service
```

完整链路分别是：

```text
Browser → Vite → Gateway → Example1
Browser → Vite → Gateway → Example2
Browser → Vite → Gateway → Example1 → Example2
Browser → Vite → Gateway → Example2 → Example1
```

## 使用 AppHost 启动

```bash
dotnet run --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

Aspire 会编排 Example1、Example2、Example3、Gateway 与 `example5-frontend`。Dashboard 固定地址为 `https://localhost:57289`，前端固定地址为 `http://localhost:5175`。在页面中执行“刷新服务状态”后，两个服务应显示“正常”；执行“调用全部”后，两条服务互调链路应显示“成功”。

`example5-frontend` 使用 `.WaitFor(gateway)`，只有 Gateway 就绪后才开始启动。Aspire 资源页不会额外保留一个已经完成的 `npm install` 资源。

## 单独启动前端

先确保 Example1、Example2 和 Gateway 已分别运行在 `5002`、`5003`、`5000`，然后执行：

```bash
cd src/SyZero.Service/SyZero.Example5.Frontend
npm install
npm run dev
```

默认访问 `http://localhost:5175`。Vite 会把 `/example1` 和 `/example2` 代理到 `http://localhost:5000`。

Vite 配置启用了 `strictPort`。如果 `5175` 被占用，启动会直接失败并提示端口冲突，不会自动换到其他端口。

## 页面操作与预期结果

| 操作 | 验证内容 | 成功标志 |
| --- | --- | --- |
| 刷新服务状态 | Gateway 到 Example1、Example2 的路由 | `2 / 2 个服务可通过网关访问` |
| Example1 → Example2 | Example1 的 Feign 服务发现和远程调用 | 状态为“成功”，远程服务为 Example2 |
| Example2 → Example1 | Example2 的 Feign 服务发现和远程调用 | 状态为“成功”，远程服务为 Example1 |
| 调用全部 | 并行执行两条服务互调链路 | 两张调用卡片均为“成功” |

所有请求都使用相对路径，因此浏览器始终访问 Vite 同源地址，再由开发代理转发给 Gateway，不需要在前端代码中硬编码跨域地址。

## 构建验证

```bash
npm run build
```

构建命令会先运行 TypeScript 严格类型检查，再生成 `dist/` 静态文件。

## 常见问题

### 页面返回 HTTP 404

检查请求路径是否以 `/example1` 或 `/example2` 开头。Example3 是单体 SPA 示例，不在当前网关路由中，也不应添加到 Example5。

### 页面能打开，但两个服务都是异常

依次确认 Gateway、Example1、Example2 是否运行在 `5000`、`5002`、`5003`，并直接访问：

```text
http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo
http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo
```

### 服务状态正常，但服务互调失败

这说明浏览器到网关的链路正常，问题位于 Example1/Example2 的 Feign 调用或服务发现。到 Aspire Dashboard 的“跟踪”页面检查是否存在 `gateway → example1 → example2` 或反向链路，再参考 [服务注册与发现](/guide/service-discovery) 排查。

## 延伸阅读

- [AppHost 本地编排与可观测性](/guide/apphost-observability)
- [SyZero.Gateway 微服务网关示例](/guide/gateway-microservice-demo)
