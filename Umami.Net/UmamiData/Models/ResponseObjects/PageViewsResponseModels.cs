namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class PageViewsResponseModel
{
    public Pageviews[] pageviews { get; set; }
    public Sessions[] sessions { get; set; }

    public class Pageviews
    {
        public string x { get; set; }
        public int y { get; set; }
    }

    public class Sessions
    {
        public string x { get; set; }
        public int y { get; set; }
    }
}