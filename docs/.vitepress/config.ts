import { defineConfig } from "vitepress";

const moduleLinks = [
  { text: "SyZero.Core", link: "/modules/SyZero.Core" },
  { text: "SyZero.Gateway.Abstractions", link: "/modules/SyZero.Gateway.Abstractions" },
  { text: "SyZero.Gateway.Ocelot", link: "/modules/SyZero.Gateway.Ocelot" },
  { text: "SyZero.Gateway.YARP", link: "/modules/SyZero.Gateway.YARP" },
  { text: "SyZero.Gateway.Admin", link: "/modules/SyZero.Gateway.Admin" },
  { text: "SyZero.AspNetCore", link: "/modules/SyZero.AspNetCore" },
  { text: "SyZero.AspNetCore.SpaProxy", link: "/modules/SyZero.AspNetCore.SpaProxy" },
  { text: "SyZero.Authentication.ApiKey", link: "/modules/SyZero.Authentication.ApiKey" },
  { text: "SyZero.Authorization", link: "/modules/SyZero.Authorization" },
  { text: "SyZero.Authorization.AspNetCore", link: "/modules/SyZero.Authorization.AspNetCore" },
  { text: "SyZero.AutoMapper", link: "/modules/SyZero.AutoMapper" },
  { text: "SyZero.AI", link: "/modules/SyZero.AI" },
  { text: "SyZero.BackgroundJobs", link: "/modules/SyZero.BackgroundJobs" },
  { text: "SyZero.BackgroundJobs.Quartz", link: "/modules/SyZero.BackgroundJobs.Quartz" },
  { text: "SyZero.Audit", link: "/modules/SyZero.Audit" },
  { text: "SyZero.Consul", link: "/modules/SyZero.Consul" },
  { text: "SyZero.DataProtection", link: "/modules/SyZero.DataProtection" },
  { text: "SyZero.Diagnostics", link: "/modules/SyZero.Diagnostics" },
  { text: "SyZero.DistributedLock", link: "/modules/SyZero.DistributedLock" },
  { text: "SyZero.DistributedTransaction", link: "/modules/SyZero.DistributedTransaction" },
  { text: "SyZero.DistributedTransaction.EntityFrameworkCore", link: "/modules/SyZero.DistributedTransaction.EntityFrameworkCore" },
  { text: "SyZero.DynamicGrpc", link: "/modules/SyZero.DynamicGrpc" },
  { text: "SyZero.DynamicWebApi", link: "/modules/SyZero.DynamicWebApi" },
  { text: "SyZero.EntityFrameworkCore", link: "/modules/SyZero.EntityFrameworkCore" },
  { text: "SyZero.EventBus.Kafka", link: "/modules/SyZero.EventBus.Kafka" },
  { text: "SyZero.FeatureManagement", link: "/modules/SyZero.FeatureManagement" },
  { text: "SyZero.Feign", link: "/modules/SyZero.Feign" },
  { text: "SyZero.Files", link: "/modules/SyZero.Files" },
  { text: "SyZero.GraphQL", link: "/modules/SyZero.GraphQL" },
  { text: "SyZero.HealthChecks", link: "/modules/SyZero.HealthChecks" },
  { text: "SyZero.Idempotency", link: "/modules/SyZero.Idempotency" },
  { text: "SyZero.Localization", link: "/modules/SyZero.Localization" },
  { text: "SyZero.Log4Net", link: "/modules/SyZero.Log4Net" },
  { text: "SyZero.Mail", link: "/modules/SyZero.Mail" },
  { text: "SyZero.MongoDB", link: "/modules/SyZero.MongoDB" },
  { text: "SyZero.MultiTenancy", link: "/modules/SyZero.MultiTenancy" },
  { text: "SyZero.MultiTenancy.EntityFrameworkCore", link: "/modules/SyZero.MultiTenancy.EntityFrameworkCore" },
  { text: "SyZero.Nacos", link: "/modules/SyZero.Nacos" },
  { text: "SyZero.OpenTelemetry", link: "/modules/SyZero.OpenTelemetry" },
  { text: "SyZero.RabbitMQ", link: "/modules/SyZero.RabbitMQ" },
  { text: "SyZero.RateLimiting", link: "/modules/SyZero.RateLimiting" },
  { text: "SyZero.Realtime", link: "/modules/SyZero.Realtime" },
  { text: "SyZero.Redis", link: "/modules/SyZero.Redis" },
  { text: "SyZero.Resilience", link: "/modules/SyZero.Resilience" },
  { text: "SyZero.Sdk", link: "/modules/SyZero.Sdk" },
  { text: "SyZero.Sms", link: "/modules/SyZero.Sms" },
  { text: "SyZero.SourceGenerator", link: "/modules/SyZero.SourceGenerator" },
  { text: "SyZero.SqlSugar", link: "/modules/SyZero.SqlSugar" },
  { text: "SyZero.Swagger", link: "/modules/SyZero.Swagger" },
  { text: "SyZero.Validation", link: "/modules/SyZero.Validation" },
  { text: "SyZero.Web.Common", link: "/modules/SyZero.Web.Common" }
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
          "/guide/": [
            {
              text: "指南",
              items: [
                { text: "快速开始", link: "/guide/getting-started" },
                { text: "架构与约定", link: "/guide/architecture" },
                { text: "Example3.SPA 单体宿主示例", link: "/guide/example3-spa" },
              ],
            },
          ],
          "/modules/": [
            {
              text: "模块目录",
              items: [
                { text: "模块总览", link: "/modules/index" },
                ...moduleLinks,
              ],
            },
          ],
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
          "/en/guide/": [
            {
              text: "Guide",
              items: [
                { text: "Getting Started", link: "/en/guide/getting-started" },
                { text: "Architecture", link: "/en/guide/architecture" },
                { text: "Example3.SPA Single-Host Sample", link: "/en/guide/example3-spa" },
              ],
            },
          ],
          "/en/modules/": [
            {
              text: "Modules",
              items: [
                { text: "Module Index", link: "/en/modules/index" },
                ...moduleLinks.map((m) => ({
                  text: m.text,
                  link: `/en${m.link}`,
                })),
              ],
            },
          ],
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