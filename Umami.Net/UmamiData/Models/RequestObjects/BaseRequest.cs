using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

public class BaseRequest
{
    [QueryStringParameter("startAt", true)]
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date

    [QueryStringParameter("endAt", true)]
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date

    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}