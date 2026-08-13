using System.Net;
using System.Net.Http.Json;
using System.Text;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Infrastructure;

public sealed class AuthDelegatingHandlerTests
{
    private const string OldAccess = "old-access-token";
    private const string OldRefresh = "old-refresh-token";
    private const string NewAccess = "new-access-token";
    private const string NewRefresh = "new-refresh-token";
    private const string Tenant = "tenant-1";

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public Func<HttpRequestMessage, Task<HttpResponseMessage>>? OnSend { get; set; }

        public List<string?> AuthHeaders { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            AuthHeaders.Add(request.Headers.Authorization?.ToString());
            if (OnSend is not null)
            {
                return await OnSend(request);
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// Pretends to be the refresh endpoint: returns a fresh token pair.
    /// </summary>
    private sealed class RefreshHandler(string token, string refreshToken) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? LastRefreshToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRefreshToken = ExtractRefreshToken(request.Content);
            var body = $$"""{"token": "{{token}}", "refreshToken": "{{refreshToken}}"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }

        private static string? ExtractRefreshToken(HttpContent? content)
        {
            if (content is null)
            {
                return null;
            }

            var json = content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("refreshToken", out var value) ? value.GetString() : null;
        }
    }

    private static (AuthDelegatingHandler Handler, ITokenStore Store, RecordingHandler Inner) Create(
        RecordingHandler inner,
        Action<ITokenStore>? seed = null)
    {
        var store = Substitute.For<ITokenStore>();
        store.GetAccessTokenAsync().Returns(OldAccess);
        store.GetRefreshTokenAsync().Returns(OldRefresh);
        store.GetTenantAsync().Returns(Tenant);
        seed?.Invoke(store);

        var handler = new AuthDelegatingHandler(store, new StubHttpClientFactory())
        {
            InnerHandler = inner,
        };
        return (handler, store, inner);
    }

    private static Task<HttpResponseMessage> SendAsync(AuthDelegatingHandler handler)
        => new HttpMessageInvoker(handler).SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/v1/anything"), CancellationToken.None);

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpMessageHandler Handler { get; set; } = new NoOpHandler();

        public HttpClient CreateClient(string name) => new(Handler)
        {
            BaseAddress = new Uri("http://localhost"),
        };

        private sealed class NoOpHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task Success_passes_through_with_bearer_header()
    {
        var inner = new RecordingHandler { OnSend = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)) };
        var (handler, _, _) = Create(inner);

        var response = await SendAsync(handler);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        inner.Calls.ShouldBe(1);
        inner.AuthHeaders[0].ShouldBe($"Bearer {OldAccess}");
    }

    [Fact]
    public async Task Unauthorized_with_refresh_token_refreshes_and_retries()
    {
        var refresh = new RefreshHandler(NewAccess, NewRefresh);
        var inner = new RecordingHandler
        {
            OnSend = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)),
        };
        var store = Substitute.For<ITokenStore>();
        store.GetAccessTokenAsync().Returns(OldAccess);
        store.GetRefreshTokenAsync().Returns(OldRefresh);
        store.GetTenantAsync().Returns(Tenant);
        var handler = new AuthDelegatingHandler(store, new StubHttpClientFactory { Handler = refresh })
        {
            InnerHandler = inner,
        };

        var response = await SendAsync(handler);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized); // second call also 401 → surfaces
        inner.Calls.ShouldBe(2);
        refresh.Calls.ShouldBe(1);
        refresh.LastRefreshToken.ShouldBe(OldRefresh);
        await store.Received(1).SetTokensAsync(NewAccess, NewRefresh);
        inner.AuthHeaders[1].ShouldBe($"Bearer {NewAccess}");
    }

    [Fact]
    public async Task Concurrent_unauthorized_request_skips_refresh_when_store_was_rotated()
    {
        // Scenario: two requests get 401 in parallel. The first refreshes and writes new
        // tokens; the second, after acquiring the lock, must NOT refresh again with the
        // now-rotated refresh token — it re-reads the store and retries with the fresh access token.
        var store = Substitute.For<ITokenStore>();
        store.GetAccessTokenAsync().Returns(OldAccess, NewAccess); // initial, re-read after lock (store already rotated)
        store.GetRefreshTokenAsync().Returns(OldRefresh);
        store.GetTenantAsync().Returns(Tenant);
        store.SetTokensAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        var first = true;
        var inner = new RecordingHandler
        {
            OnSend = _ =>
            {
                if (first)
                {
                    first = false;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
        };

        // The refresh endpoint must never be called: the store already holds the rotated token.
        var refresh = new RefreshHandler(NewAccess, NewRefresh);
        var handler = new AuthDelegatingHandler(store, new StubHttpClientFactory { Handler = refresh })
        {
            InnerHandler = inner,
        };

        var response = await SendAsync(handler);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        inner.Calls.ShouldBe(2);
        refresh.Calls.ShouldBe(0);
        await store.DidNotReceive().SetTokensAsync(Arg.Any<string>(), Arg.Any<string?>());
        inner.AuthHeaders[1].ShouldBe($"Bearer {NewAccess}");
    }

    [Fact]
    public async Task Refresh_failure_clears_session_and_throws_401()
    {
        // API says 401; the refresh endpoint itself is down (500) → session cleared.
        var inner = new RecordingHandler { OnSend = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)) };
        var refresh = new RefreshHandler(NewAccess, NewRefresh); // not reached
        var store = Substitute.For<ITokenStore>();
        store.GetAccessTokenAsync().Returns(OldAccess);
        store.GetRefreshTokenAsync().Returns(OldRefresh);
        store.GetTenantAsync().Returns(Tenant);
        var handler = new AuthDelegatingHandler(store, new StubHttpClientFactory
        {
            Handler = new FailureHandler(HttpStatusCode.InternalServerError),
        })
        {
            InnerHandler = inner,
        };

        var ex = await Should.ThrowAsync<ApiRequestException>(() => SendAsync(handler));

        ex.StatusCode.ShouldBe(401);
        await store.Received(1).ClearAsync();
        inner.Calls.ShouldBe(1);
    }

    private sealed class FailureHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status));
    }
}
