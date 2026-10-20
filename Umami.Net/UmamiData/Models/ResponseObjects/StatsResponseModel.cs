using System.Text.Json.Serialization;

﻿namespace Umami.Net.UmamiData.Models.ResponseObjects;

[JsonConverter(typeof(StatsResponseConverter))]
public class StatsResponseModels
{
    public Pageviews pageviews { get; set; } = new();
    public Visitors visitors { get; set; } = new();
    public Visits visits { get; set; } = new();
    public Bounces bounces { get; set; } = new();
    public Totaltime totaltime { get; set; } = new();


    public class Pageviews
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visitors
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visits
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Bounces
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Totaltime
    {
        public int value { get; set; }
        public int prev { get; set; }
    }
}