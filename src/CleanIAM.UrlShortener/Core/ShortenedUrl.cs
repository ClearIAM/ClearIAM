namespace CleanIAM.UrlShortener.Core;

public class ShortenedUrl
{
    public Guid Id { get; set; }
    public required string OriginalUrl { get; set; }
}