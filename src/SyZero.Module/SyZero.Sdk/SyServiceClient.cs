using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Sdk;

/// <summary>
/// SyZero SDK 客户端基类：用 typed HttpClient 发起对其他 SyZero 服务的调用，
/// 自动统一基址、JSON 序列化与错误抛出。业务侧继承本类组织自己的 SDK 方法。
/// </summary>
public abstract class SyServiceClient
{
    protected HttpClient Http { get; }

    protected SyServiceClient(HttpClient http) => Http = http;

    /// <summary>
    /// GET 调用并将 JSON 响应反序列化为 <typeparamref name="T"/>。非 2xx 直接抛出。
    /// </summary>
    protected async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var resp = await Http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// POST JSON 并将响应反序列化为 <typeparamref name="TResp"/>。
    /// </summary>
    protected async Task<TResp?> PostAsync<TReq, TResp>(string requestUri, TReq body, CancellationToken cancellationToken = default)
    {
        using var resp = await Http.PostAsJsonAsync(requestUri, body, cancellationToken).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TResp>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
