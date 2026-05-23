using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using TestProject.Interfaces;
using TestProject.Models;

namespace TestProject.Services;

public class NewsFeedService : INewsFeedService
{
    private static readonly TimeSpan FeedCacheDuration = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<NewsFeedService> _logger;

    public NewsFeedService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        ILogger<NewsFeedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsFeedItem>> GetFeedItemsAsync(string feedUrl)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return [];
        }

        var normalizedFeedUrl = feedUrl.Trim();
        var cacheKey = $"news-feed:{normalizedFeedUrl}";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<NewsFeedItem>? cachedItems))
        {
            return cachedItems ?? [];
        }

        // Cache only successful responses so a temporary feed failure does not replace working data.
        try
        {
            var client = _httpClientFactory.CreateClient("NewsFeed");
            using var response = await client.GetAsync(normalizedFeedUrl);
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
                        ImageUrl = GetImageUrl(item),
                        PublishDate = pubDate
                    };
                })
                .OrderByDescending(i => i.PublishDate)
                .ToList();

            _memoryCache.Set(cacheKey, items, FeedCacheDuration);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch news feed from {FeedUrl}", normalizedFeedUrl);
            return [];
        }
    }

    private static string GetImageUrl(XElement item)
    {
        // Accept the bespoke feed image element and common RSS image formats used by other feeds.
        var directImage = item.Element("image")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(directImage))
        {
            return directImage;
        }

        var mediaImage = item.Elements()
            .FirstOrDefault(element =>
                element.Name.LocalName is "content" or "thumbnail"
                && element.Name.NamespaceName.Contains("search.yahoo.com/mrss"))?
            .Attribute("url")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(mediaImage))
        {
            return mediaImage;
        }

        return item.Elements("enclosure")
            .FirstOrDefault(element => element.Attribute("type")?.Value.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)?
            .Attribute("url")?.Value?.Trim() ?? string.Empty;
    }
}
