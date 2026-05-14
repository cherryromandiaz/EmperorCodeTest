using System.Xml.Linq;
using TestProject.Interfaces;
using TestProject.Models;

namespace TestProject.Services;

public class NewsFeedService : INewsFeedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NewsFeedService> _logger;

    public NewsFeedService(IHttpClientFactory httpClientFactory, ILogger<NewsFeedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsFeedItem>> GetFeedItemsAsync(string feedUrl)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient("NewsFeed");
            using var response = await client.GetAsync(feedUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

            var items = doc.Descendants("item")
                .Select(item =>
                {
                    DateTime.TryParse(item.Element("pubDate")?.Value, out var pubDate);

                    return new NewsFeedItem
                    {
                        Title = item.Element("title")?.Value?.Trim() ?? string.Empty,
                        Description = item.Element("description")?.Value?.Trim() ?? string.Empty,
                        Link = item.Element("link")?.Value?.Trim() ?? string.Empty,
                        ImageUrl = item.Element("image")?.Value?.Trim() ?? string.Empty,
                        PublishDate = pubDate
                    };
                })
                .OrderByDescending(i => i.PublishDate)
                .ToList();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch news feed from {FeedUrl}", feedUrl);
            return [];
        }
    }
}
