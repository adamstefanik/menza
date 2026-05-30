namespace UTB.Minute.CanteenClient.Services;

public class TokenProvider
{
    public string? AccessToken { get; set; }
}

public class TokenHandler(TokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}