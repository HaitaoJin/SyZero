using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SyZero.Authentication.ApiKey;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace SyZero.Tests;

public class ApiKeyAuthenticationTests
{
    private static async Task<TestApiKeyHandler> CreateHandlerAsync(ApiKeyOptions options, HttpContext context)
    {
        var monitor = new TestOptionsMonitor<ApiKeyOptions>(options);
        var handler = new TestApiKeyHandler(monitor);
        await handler.InitializeAsync(new AuthenticationScheme(ApiKeyOptions.Scheme, ApiKeyOptions.Scheme, typeof(TestApiKeyHandler)), context);
        return handler;
    }

    [Fact]
    public async Task NoHeader_NoResult()
    {
        var handler = await CreateHandlerAsync(new ApiKeyOptions { Keys = { ["k"] = "u" } }, new DefaultHttpContext());
        var result = await handler.AuthenticateAsync();
        Assert.True(result.None);
    }

    [Fact]
    public async Task ValidHeader_Succeeds()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Api-Key"] = "k";
        var handler = await CreateHandlerAsync(new ApiKeyOptions { Keys = { ["k"] = "alice" } }, ctx);
        var result = await handler.AuthenticateAsync();
        Assert.True(result.Succeeded);
        Assert.Equal("alice", result.Principal!.Identity!.Name);
    }

    [Fact]
    public async Task InvalidHeader_Fails()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Api-Key"] = "bad";
        var handler = await CreateHandlerAsync(new ApiKeyOptions { Keys = { ["k"] = "alice" } }, ctx);
        var result = await handler.AuthenticateAsync();
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    private sealed class TestApiKeyHandler : AuthenticationHandler<ApiKeyOptions>
    {
        public TestApiKeyHandler(IOptionsMonitor<ApiKeyOptions> options)
            : base(options, NullLoggerFactory.Instance, UrlEncoder.Default) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Options.HeaderName, out var values) || string.IsNullOrWhiteSpace(values))
                return Task.FromResult(AuthenticateResult.NoResult());
            if (!Options.Keys.TryGetValue(values.ToString(), out var subject))
                return Task.FromResult(AuthenticateResult.Fail("invalid"));
            var id = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, subject) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme.Name)));
        }
    }

    private sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class
    {
        private readonly TOptions _value;
        public TestOptionsMonitor(TOptions value) => _value = value;
        public TOptions CurrentValue => _value;
        public TOptions Get(string? name) => _value;
        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
