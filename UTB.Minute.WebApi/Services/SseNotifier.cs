using System.Threading.Channels;

namespace UTB.Minute.WebApi.Services;

public class SseNotifier
{
    private readonly List<Channel<string>> _clients = [];

    public ChannelReader<string> Subscribe()
    {
        var channel = Channel.CreateUnbounded<string>();
        lock (_clients)
        {
            _clients.Add(channel);
        }
        return channel.Reader;
    }

    public void Unsubscribe(ChannelReader<string> reader)
    {
        lock (_clients)
        {
            var channel = _clients.FirstOrDefault(c => c.Reader == reader);
            if (channel != null)
            {
                _clients.Remove(channel);
                channel.Writer.Complete();
            }
        }
    }

    public async Task NotifyAsync(string message)
    {
        List<Channel<string>> activeClients;
        lock (_clients)
        {
            activeClients = _clients.ToList();
        }

        foreach (var client in activeClients)
        {
            await client.Writer.WriteAsync(message);
        }
    }
}