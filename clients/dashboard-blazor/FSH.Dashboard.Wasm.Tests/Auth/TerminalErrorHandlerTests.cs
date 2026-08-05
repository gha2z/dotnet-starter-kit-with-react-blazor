using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using FSH.Dashboard.Wasm.Auth;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Auth;

public sealed class TerminalErrorHandlerTests
{
    private sealed class TestNav : NavigationManager
    {
        public TestNav(string? initialPath = null)
            => Initialize("http://localhost/", $"http://localhost{initialPath ?? "/"}");

        public string? LastUri { get; private set; }

        protected override void NavigateToCore(string uri, NavigationOptions options)
            => LastUri = uri;
    }

    private sealed class StubHandler(HttpStatusCode status, string? body = null, Exception? exception = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = body is null ? new ByteArrayContent([]) : new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static string MakeToken(string? actorId)
    {
        var claims = new List<Claim>
        {
            new("tenant", "acme"),
        };
        if (actorId is not null)
        {
            claims.Add(new Claim("act_sub", actorId));
        }

        var jwt = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(5));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static (TerminalErrorHandler Handler, TestNav Nav) Create(
        ITokenStore tokenStore,
        StubHandler inner,
        string? initialPath = null)
    {
        var nav = new TestNav(initialPath);
        var handler = new TerminalErrorHandler(tokenStore, nav) { InnerHandler = inner };
        return (handler, nav);
    }

    private static HttpRequestMessage Request() => new(HttpMethod.Get, "http://localhost/api/v1/anything");

    // DelegatingHandler.SendAsync is protected - HttpMessageInvoker exposes it publicly.
    private static Task<HttpResponseMessage> SendAsync(TerminalErrorHandler handler, HttpRequestMessage request)
        => new HttpMessageInvoker(handler).SendAsync(request, CancellationToken.None);

    [Fact]
    public async Task Deactivated_tenant_403_routes_to_terminal_page_and_preserves_body()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: "op1"));
        var inner = new StubHandler(HttpStatusCode.Forbidden, """{"statusCode":403,"message":"This tenant has been deactivated. Contact your administrator."}""");
        var (handler, nav) = Create(tokenStore, inner);

        var response = await SendAsync(handler, Request());

        nav.LastUri.ShouldBe("/tenant-deactivated");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).ShouldContain("tenant has been deactivated");
    }

    [Fact]
    public async Task Forbidden_without_deactivation_reason_is_not_routed()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: "op1"));
        var inner = new StubHandler(HttpStatusCode.Forbidden, """{"statusCode":403,"message":"You do not have permission."}""");
        var (handler, nav) = Create(tokenStore, inner);

        var response = await SendAsync(handler, Request());

        nav.LastUri.ShouldBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthorized_while_impersonating_routes_to_impersonation_ended()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: "op1"));
        var (handler, nav) = Create(tokenStore, new StubHandler(HttpStatusCode.Unauthorized));

        var response = await SendAsync(handler, Request());

        nav.LastUri.ShouldBe("/impersonation-ended");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Plain_401_without_actor_claims_is_not_routed()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: null));
        var (handler, nav) = Create(tokenStore, new StubHandler(HttpStatusCode.Unauthorized));

        var response = await SendAsync(handler, Request());

        nav.LastUri.ShouldBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Successful_responses_are_not_routed()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: "op1"));
        var (handler, nav) = Create(tokenStore, new StubHandler(HttpStatusCode.OK, """{"ok":true}"""));

        var response = await SendAsync(handler, Request());

        nav.LastUri.ShouldBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task No_route_when_already_on_a_terminal_page()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: "op1"));
        var inner = new StubHandler(HttpStatusCode.Forbidden, """{"message":"This tenant has been deactivated."}""");
        var (handler, nav) = Create(tokenStore, inner, initialPath: "/tenant-deactivated");

        await SendAsync(handler, Request());

        nav.LastUri.ShouldBeNull();
    }

    [Fact]
    public async Task ApiRequestException_401_while_impersonating_routes_and_rethrows()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: "op1"));
        var inner = new StubHandler(HttpStatusCode.InternalServerError, exception: new ApiRequestException(401, "Session expired"));
        var (handler, nav) = Create(tokenStore, inner);

        await Should.ThrowAsync<ApiRequestException>(() => SendAsync(handler, Request()));

        nav.LastUri.ShouldBe("/impersonation-ended");
    }

    [Fact]
    public async Task ApiRequestException_401_without_impersonation_is_rethrown_without_routing()
    {
        var tokenStore = Substitute.For<ITokenStore>();
        tokenStore.GetAccessTokenAsync().Returns(MakeToken(actorId: null));
        var inner = new StubHandler(HttpStatusCode.InternalServerError, exception: new ApiRequestException(401, "Session expired"));
        var (handler, nav) = Create(tokenStore, inner);

        await Should.ThrowAsync<ApiRequestException>(() => SendAsync(handler, Request()));

        nav.LastUri.ShouldBeNull();
    }
}
