#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SyZero.Gateway.Admin.Controllers
{
    /// <summary>
    /// AdminController 限流 / 负载均衡管理端点（拆分为 partial 以保持主文件可读）。
    /// </summary>
    public partial class AdminController
    {
        // 形如 "1s" / "5m" / "1h" / "1d"
        private static readonly Regex PeriodRegex = new("^[1-9][0-9]*[smhd]$", RegexOptions.Compiled);

        private static readonly HashSet<string> ValidLoadBalancerTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            GatewayLoadBalancerOptions.RoundRobin,
            GatewayLoadBalancerOptions.LeastConnection,
            GatewayLoadBalancerOptions.NoLoadBalance,
        };

        // ============== 单路由限流 ==============

        /// <summary>读取单路由限流配置。null 表示该路由未配置限流。</summary>
        [HttpGet("routes/{index:int}/rate-limit")]
        public async Task<IActionResult> GetRouteRateLimit(int index, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count) return NotFound();
            return Ok(dto.Routes[index].RateLimitOptions);
        }

        /// <summary>设置或更新单路由限流配置。</summary>
        [HttpPut("routes/{index:int}/rate-limit")]
        public async Task<IActionResult> SetRouteRateLimit(int index, [FromBody] GatewayRateLimitOptions opts, CancellationToken ct)
        {
            if (opts is null) return BadRequest(new { title = "请求体不能为空" });
            if (opts.EnableRateLimiting)
            {
                if (string.IsNullOrWhiteSpace(opts.Period) || !PeriodRegex.IsMatch(opts.Period!))
                {
                    return BadRequest(new { title = "Period 必须形如 1s/5m/1h/1d" });
                }
                if (opts.Limit is not > 0)
                {
                    return BadRequest(new { title = "Limit 必须为正整数" });
                }
            }
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count) return NotFound();
            dto.Routes[index].RateLimitOptions = opts;
            await _store.WriteAsync(dto, ct);
            await Audit("ratelimit.route.update", dto.Routes[index].UpstreamPathTemplate,
                $"路由 [{index}] {dto.Routes[index].UpstreamPathTemplate} 限流：Enable={opts.EnableRateLimiting}, Limit={opts.Limit}, Period={opts.Period}",
                ct);
            return NoContent();
        }

        /// <summary>清除单路由限流配置。</summary>
        [HttpDelete("routes/{index:int}/rate-limit")]
        public async Task<IActionResult> ClearRouteRateLimit(int index, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count) return NotFound();
            dto.Routes[index].RateLimitOptions = null;
            await _store.WriteAsync(dto, ct);
            await Audit("ratelimit.route.clear", dto.Routes[index].UpstreamPathTemplate,
                $"清除路由 [{index}] {dto.Routes[index].UpstreamPathTemplate} 限流配置", ct);
            return NoContent();
        }

        // ============== 全局限流 ==============

        [HttpGet("rate-limit/global")]
        public async Task<IActionResult> GetGlobalRateLimit(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            return Ok(dto.GetGlobalRateLimit());
        }

        [HttpPut("rate-limit/global")]
        public async Task<IActionResult> SetGlobalRateLimit([FromBody] GatewayGlobalRateLimitOptions opts, CancellationToken ct)
        {
            if (opts is null) return BadRequest(new { title = "请求体不能为空" });
            if (opts.HttpStatusCode is < 100 or > 599)
            {
                return BadRequest(new { title = "HttpStatusCode 必须在 100-599 之间" });
            }
            var dto = await _store.ReadAsync(ct);
            dto.SetGlobalRateLimit(opts);
            await _store.WriteAsync(dto, ct);
            await Audit("ratelimit.global.update", null,
                $"全局限流：DisableHeaders={opts.DisableRateLimitHeaders}, Status={opts.HttpStatusCode}, Header={opts.ClientIdHeader}",
                ct);
            return NoContent();
        }

        [HttpDelete("rate-limit/global")]
        public async Task<IActionResult> ClearGlobalRateLimit(CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            dto.SetGlobalRateLimit(null);
            await _store.WriteAsync(dto, ct);
            await Audit("ratelimit.global.clear", null, "清除全局限流配置", ct);
            return NoContent();
        }

        // ============== 单路由负载均衡 ==============

        [HttpGet("routes/{index:int}/load-balancer")]
        public async Task<IActionResult> GetRouteLoadBalancer(int index, CancellationToken ct)
        {
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count) return NotFound();
            return Ok(dto.Routes[index].LoadBalancerOptions);
        }

        [HttpPut("routes/{index:int}/load-balancer")]
        public async Task<IActionResult> SetRouteLoadBalancer(int index, [FromBody] GatewayLoadBalancerOptions opts, CancellationToken ct)
        {
            if (opts is null) return BadRequest(new { title = "请求体不能为空" });
            if (string.IsNullOrWhiteSpace(opts.Type) || !ValidLoadBalancerTypes.Contains(opts.Type))
            {
                return BadRequest(new { title = "Type 只能是 RoundRobin / LeastConnection / NoLoadBalance" });
            }
            var dto = await _store.ReadAsync(ct);
            if (index < 0 || index >= dto.Routes.Count) return NotFound();
            dto.Routes[index].LoadBalancerOptions = opts;
            await _store.WriteAsync(dto, ct);
            await Audit("loadbalancer.route.update", dto.Routes[index].UpstreamPathTemplate,
                $"路由 [{index}] {dto.Routes[index].UpstreamPathTemplate} 负载均衡 → {opts.Type}", ct);
            return NoContent();
        }

        // ============== 元数据辅助 ==============

        /// <summary>返回支持的负载均衡器类型列表（前端下拉用）。</summary>
        [HttpGet("load-balancer/types")]
        public IActionResult GetLoadBalancerTypes()
            => Ok(new[]
            {
                GatewayLoadBalancerOptions.RoundRobin,
                GatewayLoadBalancerOptions.LeastConnection,
                GatewayLoadBalancerOptions.NoLoadBalance,
            });
    }
}
