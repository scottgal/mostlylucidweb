namespace Umami.Net.UmamiData.Models.ResponseObjects;

public sealed record Annotation
{
    public string Id { get; init; } = "";
    public string WebsiteId { get; init; } = "";
    public string? UserId { get; init; }
    public DateTimeOffset Date { get; init; }
    public bool AllDay { get; init; }
    public string Note { get; init; } = "";
    public DateTimeOffset? CreatedAt { get; init; }
}

public sealed record AnnotationInput(DateTimeOffset Date, string Note, bool AllDay = true)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Note) || Note.Length > 500)
            throw new ArgumentException("Annotation note must contain 1–500 characters", nameof(Note));
    }
}

public sealed class PagedResponse<T>
{
    public T[] Data { get; set; } = [];
    public int Count { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool? IsCapped { get; set; }
}
