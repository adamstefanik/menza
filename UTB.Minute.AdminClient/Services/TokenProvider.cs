namespace UTB.Minute.AdminClient.Services;

public class TokenProvider
{
    private string? _accessToken;
    public string? AccessToken 
    { 
        get => _accessToken;
        set 
        {
            _accessToken = value;
            if (!string.IsNullOrEmpty(value))
                Console.WriteLine($"[AUTH] TokenProvider: Access token has been SET (Length: {value.Length})");
        }
    }
}

public class TokenHandler(TokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
            var tokenSnippet = tokenProvider.AccessToken.Length > 10 ? tokenProvider.AccessToken[..10] + "..." : "short-token";
            Console.WriteLine($"[AUTH] Token attached to {request.Method} {request.RequestUri} (Snippet: {tokenSnippet})");
        }
        else
        {
            Console.WriteLine($"[AUTH] WARNING: No token found in TokenProvider for {request.Method} {request.RequestUri}");
        }
        return await base.SendAsync(request, cancellationToken);
    }
}