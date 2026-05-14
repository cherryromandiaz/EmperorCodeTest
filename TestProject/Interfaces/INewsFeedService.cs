using TestProject.Models;

namespace TestProject.Interfaces;

public interface INewsFeedService
{
    Task<IReadOnlyList<NewsFeedItem>> GetFeedItemsAsync(string feedUrl);
}
