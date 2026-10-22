import { defineConfig } from "vitepress";

const guideSidebar = [
  {
    text: "基础入门",
    items: [
      { text: "快速开始", link: "/guide/getting-started" },
      { text: "架构与约定", link: "/guide/architecture" },
      { text: "创建分层业务服务", link: "/guide/layered-service" },
    ],
  },
  {
    text: "核心能力",
    items: [
      { text: "数据访问与仓储", link: "/guide/data-access" },
      { text: "服务注册与发现", link: "/guide/service-discovery" },
      { text: "事件总线与集成事件", link: "/guide/event-bus" },
    ],
  },
  {
    text: "示例实践",
    items: [
      { text: "Example1 + Example2 微服务互调示例", link: "/guide/example1-example2-microservices" },
      { text: "SyZero.Gateway 微服务网关示例", link: "/guide/gateway-microservice-demo" },
      { text: "Example3.SPA 单体宿主示例", link: "/guide/example3-spa" },
      { text: "Example4.Monolith 后端单体项目示例", link: "/guide/example4-monolith" },
    ],
  },
];

const enGuideSidebar = [
  {
    text: "Basics",
    items: [
      { text: "Getting Started", link: "/en/guide/getting-started" },
      { text: "Architecture", link: "/en/guide/architecture" },
      { text: "Building a Layered Service", link: "/en/guide/layered-service" },
    ],
  },
  {
    text: "Core Capabilities",
    items: [
      { text: "Data Access and Repositories", link: "/en/guide/data-access" },
      { text: "Service Registration and Discovery", link: "/en/guide/service-discovery" },
      { text: "Event Bus and Integration Events", link: "/en/guide/event-bus" },
    ],
  },
  {
    text: "Samples",
    items: [
      { text: "Example1 + Example2 Microservice Call Demo", link: "/en/guide/example1-example2-microservices" },
      { text: "SyZero.Gateway Microservice Gateway Demo", link: "/en/guide/gateway-microservice-demo" },
      { text: "Example3.SPA Single-Host Sample", link: "/en/guide/example3-spa" },
      { text: "Example4.Monolith Back-End Monolith Sample", link: "/en/guide/example4-monolith" },
    ],
  },
];

const moduleGroups = [
  {
    text: "基础与 Web",
    enText: "Core and Web",
    items: [
      { text: "SyZero.Core", link: "/modules/SyZero.Core" },
      { text: "SyZero.AspNetCore", link: "/modules/SyZero.AspNetCore" },
      { text: "SyZero.Web.Common", link: "/modules/SyZero.Web.Common" },
      { text: "SyZero.AspNetCore.SpaProxy", link: "/modules/SyZero.AspNetCore.SpaProxy" },
      { text: "SyZero.Swagger", link: "/modules/SyZero.Swagger" },
    ],
  },
  {
    text: "应用、安全与租户",
    enText: "Application, Security and Tenancy",
    items: [
      { text: "SyZero.Authorization", link: "/modules/SyZero.Authorization" },
      { text: "SyZero.Authorization.AspNetCore", link: "/modules/SyZero.Authorization.AspNetCore" },
      { text: "SyZero.Authentication.ApiKey", link: "/modules/SyZero.Authentication.ApiKey" },
      { text: "SyZero.MultiTenancy", link: "/modules/SyZero.MultiTenancy" },
      { text: "SyZero.MultiTenancy.EntityFrameworkCore", link: "/modules/SyZero.MultiTenancy.EntityFrameworkCore" },
      { text: "SyZero.Validation", link: "/modules/SyZero.Validation" },
      { text: "SyZero.Audit", link: "/modules/SyZero.Audit" },
      { text: "SyZero.DataProtection", link: "/modules/SyZero.DataProtection" },
      { text: "SyZero.Idempotency", link: "/modules/SyZero.Idempotency" },
      { text: "SyZero.RateLimiting", link: "/modules/SyZero.RateLimiting" },
    ],
  },
  {
    text: "数据、缓存与事务",
    enText: "Data, Cache and Transactions",
    items: [
      { text: "SyZero.EntityFrameworkCore", link: "/modules/SyZero.EntityFrameworkCore" },
      { text: "SyZero.SqlSugar", link: "/modules/SyZero.SqlSugar" },
      { text: "SyZero.MongoDB", link: "/modules/SyZero.MongoDB" },
      { text: "SyZero.Redis", link: "/modules/SyZero.Redis" },
      { text: "SyZero.DistributedLock", link: "/modules/SyZero.DistributedLock" },
      { text: "SyZero.DistributedTransaction", link: "/modules/SyZero.DistributedTransaction" },
      { text: "SyZero.DistributedTransaction.EntityFrameworkCore", link: "/modules/SyZero.DistributedTransaction.EntityFrameworkCore" },
    ],
  },
  {
    text: "服务治理与通信",
    enText: "Governance and Communication",
    items: [
      { text: "SyZero.Gateway.Abstractions", link: "/modules/SyZero.Gateway.Abstractions" },
      { text: "SyZero.Gateway.Ocelot", link: "/modules/SyZero.Gateway.Ocelot" },
      { text: "SyZero.Gateway.YARP", link: "/modules/SyZero.Gateway.YARP" },
      { text: "SyZero.Gateway.Admin", link: "/modules/SyZero.Gateway.Admin" },
      { text: "SyZero.ServiceDiscovery.Consul", link: "/modules/SyZero.ServiceDiscovery.Consul" },
      { text: "SyZero.ServiceDiscovery.Nacos", link: "/modules/SyZero.ServiceDiscovery.Nacos" },
      { text: "SyZero.Feign", link: "/modules/SyZero.Feign" },
      { text: "SyZero.DynamicWebApi", link: "/modules/SyZero.DynamicWebApi" },
      { text: "SyZero.DynamicGrpc", link: "/modules/SyZero.DynamicGrpc" },
      { text: "SyZero.EventBus.RabbitMQ", link: "/modules/SyZero.EventBus.RabbitMQ" },
      { text: "SyZero.EventBus.Kafka", link: "/modules/SyZero.EventBus.Kafka" },
      { text: "SyZero.Realtime", link: "/modules/SyZero.Realtime" },
      { text: "SyZero.Resilience", link: "/modules/SyZero.Resilience" },
      { text: "SyZero.Sdk", link: "/modules/SyZero.Sdk" },
    ],
  },
  {
    text: "可观测与运维",
    enText: "Observability and Operations",
    items: [
      { text: "SyZero.HealthChecks", link: "/modules/SyZero.HealthChecks" },
      { text: "SyZero.Diagnostics", link: "/modules/SyZero.Diagnostics" },
      { text: "SyZero.Logging.Log4Net", link: "/modules/SyZero.Logging.Log4Net" },
      { text: "SyZero.Observability.OpenTelemetry", link: "/modules/SyZero.Observability.OpenTelemetry" },
      { text: "SyZero.BackgroundJobs", link: "/modules/SyZero.BackgroundJobs" },
      { text: "SyZero.BackgroundJobs.Quartz", link: "/modules/SyZero.BackgroundJobs.Quartz" },
      { text: "SyZero.FeatureManagement", link: "/modules/SyZero.FeatureManagement" },
      { text: "SyZero.Localization", link: "/modules/SyZero.Localization" },
    ],
  },
  {
    text: "生态与工具",
    enText: "Ecosystem and Tooling",
    items: [
      { text: "SyZero.ObjectMapping.Mapster", link: "/modules/SyZero.ObjectMapping.Mapster" },
      { text: "SyZero.ObjectMapping.AutoMapper", link: "/modules/SyZero.ObjectMapping.AutoMapper" },
      { text: "SyZero.Files", link: "/modules/SyZero.Files" },
      { text: "SyZero.Mail", link: "/modules/SyZero.Mail" },
      { text: "SyZero.Sms", link: "/modules/SyZero.Sms" },
      { text: "SyZero.GraphQL", link: "/modules/SyZero.GraphQL" },
      { text: "SyZero.AI", link: "/modules/SyZero.AI" },
      { text: "SyZero.SourceGenerator", link: "/modules/SyZero.SourceGenerator" },
    ],
  },
];

const moduleSidebar = [
  { text: "模块总览", link: "/modules/index" },
  ...moduleGroups.map(({ text, items }) => ({
    text,
    collapsed: false,
    items,
  })),
];

const enModuleSidebar = [
  { text: "Module Index", link: "/en/modules/index" },
  ...moduleGroups.map(({ enText, items }) => ({
    text: enText,
    collapsed: false,
    items: items.map((item) => ({
      text: item.text,
      link: `/en${item.link}`,
    })),
  })),
];

export default defineConfig({
  title: "SyZero",
  description: "轻量级、模块化 .NET 微服务开发框架文档",
  head: [
    ["link", { rel: "icon", href: "/icon/logo.png" }],
    ["link", { rel: "apple-touch-icon", href: "/icon/logo.png" }],
  ],
  cleanUrls: true,
  ignoreDeadLinks: true,
  themeConfig: {
    logo: "/icon/logo.png",
    outline: { level: [2, 3], label: "页面导航" },
    socialLinks: [
      { icon: "github", link: "https://github.com/HaitaoJin/SyZero" },
    ],
    search: {
      provider: "local"
    },
    footer: {
      copyright: '© Copyright 2026 SyZero&nbsp;&nbsp;<strong><a href="https://beian.miit.gov.cn/" target="_blank" rel="noopener noreferrer">陕ICP备19004355号-1</a></strong>',
    },
  },
  locales: {
    root: {
      label: "简体中文",
      lang: "zh-CN",
      link: "/",
      themeConfig: {
        nav: [
          { text: "指南", link: "/guide/getting-started" },
          { text: "模块", link: "/modules/index" },
          { text: "发布说明", link: "/release-notes" },
          { text: "GitHub", link: "https://github.com/HaitaoJin/SyZero" },
        ],
        sidebar: {
          "/guide/": guideSidebar,
          "/modules/": moduleSidebar,
        },
        docFooter: { prev: "上一页", next: "下一页" },
        darkModeSwitchLabel: "主题",
        sidebarMenuLabel: "目录",
        returnToTopLabel: "返回顶部",
        langMenuLabel: "多语言",
      },
    },
    en: {
      label: "English",
      lang: "en-US",
      link: "/en/",
      themeConfig: {
        nav: [
          { text: "Guide", link: "/en/guide/getting-started" },
          { text: "Modules", link: "/en/modules/index" },
          { text: "Release Notes", link: "/en/release-notes" },
          { text: "GitHub", link: "https://github.com/HaitaoJin/SyZero" },
        ],
        sidebar: {
          "/en/guide/": enGuideSidebar,
          "/en/modules/": enModuleSidebar,
        },
        docFooter: { prev: "Previous page", next: "Next page" },
        darkModeSwitchLabel: "Theme",
        sidebarMenuLabel: "Menu",
        returnToTopLabel: "Back to top",
        langMenuLabel: "Languages",
      },
    },
  },
});
