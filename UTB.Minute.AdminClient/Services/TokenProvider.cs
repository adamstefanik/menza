using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace UTB.Minute.AdminClient.Services;

public class TokenProvider
{
    public string? AccessToken { get; set; }
}

public class TokenHandler(IHttpContextAccessor httpContextAccessor, TokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? accessToken = null;

        // 1. Try to get token from HttpContext (Works during SSR / initial load)
        var ctx = httpContextAccessor.HttpContext;
        if (ctx?.User?.Identity?.IsAuthenticated == true)
        {
            accessToken = await ctx.GetTokenAsync("access_token");
        }

        // 2. Fallback to TokenProvider (Works during Interactive session after persistence logic in Routes.razor runs)
        if (string.IsNullOrEmpty(accessToken))
        {
            accessToken = tokenProvider.AccessToken;
        }

        // 3. Attach the token if found
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            Console.WriteLine($"[DEBUG] FULL ACCESS TOKEN: {accessToken}");
            var snippet = accessToken.Length > 10 ? accessToken[..10] + "..." : "token";
            Console.WriteLine($"[AUTH] Token attached to {request.Method} {request.RequestUri} (Snippet: {snippet})");
        }
        else
        {
            // Only log warning for non-GET requests to avoid noise for public data
            if (request.Method != HttpMethod.Get)
            {
                 Console.WriteLine($"[AUTH] WARNING: No token found for {request.Method} {request.RequestUri}");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
