using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace UTB.Minute.CanteenClient.Services;

public class TokenProvider
{
    public string? AccessToken { get; set; }
}

public class TokenHandler(IHttpContextAccessor httpContextAccessor, TokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? accessToken = null;

        var ctx = httpContextAccessor.HttpContext;
        if (ctx?.User?.Identity?.IsAuthenticated == true)
        {
            accessToken = await ctx.GetTokenAsync("access_token");
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            accessToken = tokenProvider.AccessToken;
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
