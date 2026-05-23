namespace TestProject.Models;

public class NewsFeedItem
{
    public const string FallbackImageUrl = "/images/news-placeholder.svg";

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DisplayImageUrl => string.IsNullOrWhiteSpace(ImageUrl) ? FallbackImageUrl : ImageUrl;
    public DateTime PublishDate { get; set; }
}
