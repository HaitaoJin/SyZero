# SyZero.Example5.Frontend

独立 TypeScript 前端示例，通过 `SyZero.Gateway` 统一访问 `SyZero.Example1` 与 `SyZero.Example2`。

页面包含两类调用示例：

- 服务状态：浏览器经 Gateway 分别获取 Example1、Example2 的服务信息。
- 服务互调：浏览器经 Gateway 请求 Example1 调用 Example2，以及 Example2 调用 Example1，展示 Feign 服务发现与远程调用结果。

服务互调接口：

- `/example1/api/SyZero.Example1/Example1/CallExample2Service`
- `/example2/api/SyZero.Example2/Example2/CallExample1Service`

```bash
npm install
npm run dev
```

固定地址为 `http://localhost:5175`，直接运行或通过 `SyZero.AppHost` 启动时保持一致。开发服务器会把 `/example1` 与 `/example2` 请求代理到 `http://localhost:5000`。
