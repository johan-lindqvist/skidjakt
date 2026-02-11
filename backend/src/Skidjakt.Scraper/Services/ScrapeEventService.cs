using System.Collections.Concurrent;
using System.Text.Json;

namespace Skidjakt.Scraper.Services;

public class ScrapeEventService
{
    private readonly ConcurrentDictionary<string, StreamWriter> _clients = new();

    public string Subscribe(StreamWriter writer)
    {
        var id = Guid.NewGuid().ToString();
        _clients.TryAdd(id, writer);
        return id;
    }

    public void Unsubscribe(string id)
    {
        _clients.TryRemove(id, out _);
    }

    public void NotifyDealsUpdated(string agency, int count)
    {
        var data = JsonSerializer.Serialize(
            new
            {
                agency,
                count,
                timestamp = DateTime.UtcNow,
            }
        );
        foreach (var (id, writer) in _clients)
        {
            try
            {
                writer.WriteLine($"event: deals-updated");
                writer.WriteLine($"data: {data}");
                writer.WriteLine();
                writer.Flush();
            }
            catch
            {
                _clients.TryRemove(id, out _);
            }
        }
    }
}
