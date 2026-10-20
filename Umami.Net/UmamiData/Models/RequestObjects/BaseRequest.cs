using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>
/// Base request object for all Umami API data requests requiring a time range.
/// </summary>
/// <remarks>
/// All requests must specify a valid date range where StartAtDate is before EndAtDate.
/// Dates are converted to Unix millisecond timestamps for the API.
/// </remarks>
public class BaseRequest
{
    [QueryStringParameter("distinctId")] public string? DistinctId { get; set; }
    [QueryStringParameter("utmSource")] public string? UtmSource { get; set; }
    [QueryStringParameter("utmMedium")] public string? UtmMedium { get; set; }
    [QueryStringParameter("utmCampaign")] public string? UtmCampaign { get; set; }
    [QueryStringParameter("utmContent")] public string? UtmContent { get; set; }
    [QueryStringParameter("utmTerm")] public string? UtmTerm { get; set; }
    [QueryStringParameter("eventType")] public int? EventType { get; set; }
    [QueryStringParameter("excludeBounce")] public string? ExcludeBounce { get; set; }
    [QueryStringParameter("match")] public string? Match { get; set; }

    private DateTime _startAtDate;
    private DateTime _endAtDate;

    /// <summary>
    /// Gets the start timestamp in milliseconds (Unix time).
    /// This is automatically calculated from <see cref="StartAtDate"/>.
    /// </summary>
    [QueryStringParameter("startAt", true)]
    public long StartAt => StartAtDate.ToMilliseconds();

    /// <summary>
    /// Gets the end timestamp in milliseconds (Unix time).
    /// This is automatically calculated from <see cref="EndAtDate"/>.
    /// </summary>
    [QueryStringParameter("endAt", true)]
    public long EndAt => EndAtDate.ToMilliseconds();

    /// <summary>
    /// Gets or sets the start date for the data range.
    /// Must be before <see cref="EndAtDate"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the start date is after the end date.
    /// Suggestion: Ensure StartAtDate is before EndAtDate.
    /// </exception>
    public DateTime StartAtDate
    {
        get => _startAtDate;
        set
        {
            if (_endAtDate != default && value > _endAtDate)
            {
                throw new ArgumentException(
                    $"StartAtDate ({value:O}) must be before EndAtDate ({_endAtDate:O}). " +
                    "Suggestion: Set StartAtDate to an earlier date or adjust EndAtDate.",
                    nameof(StartAtDate));
            }
            _startAtDate = value;
        }
    }

    /// <summary>
    /// Gets or sets the end date for the data range.
    /// Must be after <see cref="StartAtDate"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the end date is before the start date.
    /// Suggestion: Ensure EndAtDate is after StartAtDate.
    /// </exception>
    public DateTime EndAtDate
    {
        get => _endAtDate;
        set
        {
            if (_startAtDate != default && value < _startAtDate)
            {
                throw new ArgumentException(
                    $"EndAtDate ({value:O}) must be after StartAtDate ({_startAtDate:O}). " +
                    "Suggestion: Set EndAtDate to a later date or adjust StartAtDate.",
                    nameof(EndAtDate));
            }
            _endAtDate = value;
        }
    }

    /// <summary>
    /// Validates that the request has valid date ranges.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when dates are invalid or not set.
    /// </exception>
    public virtual void Validate()
    {
        if (Match is not (null or "all" or "any")) throw new ArgumentException("Match must be all or any", nameof(Match));
        if (EventType is <= 0) throw new ArgumentOutOfRangeException(nameof(EventType));
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(this,
            new System.ComponentModel.DataAnnotations.ValidationContext(this), validateAllProperties: true);
        if (StartAtDate == default)
        {
            throw new InvalidOperationException(
                "StartAtDate is required. Suggestion: Set StartAtDate to a valid date " +
                "(e.g., DateTime.UtcNow.AddDays(-7) for last 7 days).");
        }

        if (EndAtDate == default)
        {
            throw new InvalidOperationException(
                "EndAtDate is required. Suggestion: Set EndAtDate to a valid date " +
                "(e.g., DateTime.UtcNow for current time).");
        }

        if (StartAtDate > EndAtDate)
        {
            throw new InvalidOperationException(
                $"StartAtDate ({StartAtDate:O}) must be before EndAtDate ({EndAtDate:O}). " +
                "Suggestion: Swap the dates or use a valid time range.");
        }
    }
}