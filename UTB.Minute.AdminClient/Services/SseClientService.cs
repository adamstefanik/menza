using Microsoft.AspNetCore.Components;

namespace UTB.Minute.AdminClient.Services;

public class SseClientService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;

    public event Action? OnNotificationReceived;

    public SseClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("SseClient");
        _httpClient.BaseAddress = new Uri("http://webapi");
    }

    public async Task StartAsync()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var stream = await _httpClient.GetStreamAsync("/api/notifications/sse", _cts.Token);
                    using var reader = new StreamReader(stream);
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;
                        if (line.StartsWith("data: "))
                        {
                            OnNotificationReceived?.Invoke();
                        }
                    }
                }
                catch
                {
                    await Task.Delay(2000, _cts.Token); // reconnect delay
                }
            }
        }, _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}