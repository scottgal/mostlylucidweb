﻿using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Web;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.Test.UmamiData;

public class UmamiDataDelegatingHandler : DelegatingHandler
{
    private static readonly string[] EventNames = { "RSS", "Event 1", "Event 2", "Event 3", "Event 4" };

    private static readonly string[] Urls = { "/page1", "/page2", "/page3", "/page4", "/" };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var absPath = request.RequestUri.AbsolutePath;
        switch (absPath)
        {
            case "/api/auth/login":
                var authContent = await request.Content.ReadFromJsonAsync<AuthRequest>(cancellationToken);
                if (authContent?.username == "username" && authContent?.password == "password")
                    return ReturnAuthenticatedMessage();
                if (authContent?.username == "bad")
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            case "/api/auth/verify":
                //To allow Login to continue.
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            case "/api/auth/verify?test":
                //To allow Login to continue fail.
                return new HttpResponseMessage(HttpStatusCode.OK);
            default:

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest>(request);

                    return ReturnPageViewsMessage(pageViews);
                }

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/metrics"))
                {
                    var metricsRequest = GetParams<MetricsRequest>(request);
                    return ReturnMetrics(metricsRequest);
                }

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/active"))
                {
                    var activeUsers = new ActiveUsersResponse
                    {
                        ActiveUsers = 10
                    };
                    var json = JsonSerializer.Serialize(activeUsers);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private static T GetParams<T>(HttpRequestMessage requestMessage) where T : BaseRequest, new()
    {
        var queryParams = HttpUtility.ParseQueryString(requestMessage.RequestUri.Query);
        var startAt = queryParams["startAt"];
        var startDate = DateTime.Now;
        if (long.TryParse(startAt, out var startMs))
            startDate = DateTimeOffset.FromUnixTimeMilliseconds(startMs).DateTime;

        var endAt = queryParams["endAt"];
        var endDate = DateTime.Now;
        if (long.TryParse(endAt, out var endMs)) endDate = DateTimeOffset.FromUnixTimeMilliseconds(endMs).DateTime;


        var baseRequest = new T
        {
            StartAtDate = startDate,
            EndAtDate = endDate
        };
        baseRequest = baseRequest switch
        {
            MetricsRequest metricsRequest => FillMetricsRequest(metricsRequest, queryParams) as T,
            PageViewsRequest pageViewsRequest => FillPageViewsRequest(pageViewsRequest, queryParams) as T,
            _ => baseRequest
        };
        return baseRequest;
    }

    private static PageViewsRequest FillPageViewsRequest(PageViewsRequest request, NameValueCollection queryParams)
    {
        request.Url = queryParams["url"];
        request.Referrer = queryParams["referrer"];
        request.Title = queryParams["title"];
        request.Host = queryParams["host"];
        request.Os = queryParams["os"];
        request.Browser = queryParams["browser"];
        request.Device = queryParams["device"];
        request.Country = queryParams["country"];
        request.Region = queryParams["region"];
        request.City = queryParams["city"];
        return request;
    }


    private static MetricsRequest FillMetricsRequest(MetricsRequest request, NameValueCollection queryParams)
    {
        var type = queryParams["type"];
        request.Type = Enum.Parse<MetricType>(type);
        request.Url = queryParams["url"];
        request.Referrer = queryParams["referrer"];
        request.Title = queryParams["title"];
        request.Query = queryParams["query"];
        request.Host = queryParams["host"];
        request.Os = queryParams["os"];
        request.Browser = queryParams["browser"];
        request.Device = queryParams["device"];
        request.Country = queryParams["country"];
        request.Region = queryParams["region"];
        request.City = queryParams["city"];
        request.Language = queryParams["language"];
        request.Event = queryParams["event"];
        if (int.TryParse(queryParams["limit"], out var limit)) request.Limit = limit;
        return request;
    }

    private static HttpResponseMessage ReturnMetrics(MetricsRequest request)
    {
        var itemsList = Array.Empty<string>();
        if (request.Type == MetricType.@event) itemsList = EventNames;

        if (request.Type == MetricType.url) itemsList = Urls;
        var metricsList = new List<MetricsResponseModels>();
        for (var i = 0; i < itemsList.Length; i++)
            metricsList.Add(new MetricsResponseModels
            {
                x = itemsList[i],
                y = i * 2
            });

        var json = JsonSerializer.Serialize(metricsList);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage ReturnPageViewsMessage(PageViewsRequest request)
    {
        var startAt = request.StartAt;
        var endAt = request.EndAt;
        var startDate = DateTimeOffset.FromUnixTimeMilliseconds(startAt).DateTime;
        var endDate = DateTimeOffset.FromUnixTimeMilliseconds(endAt).DateTime;
        var days = (endDate - startDate).Days;

        var pageViewsList = new List<PageViewsResponseModel.Pageviews>();
        var sessionsList = new List<PageViewsResponseModel.Sessions>();
        for (var i = 0; i < days; i++)
        {
            pageViewsList.Add(new PageViewsResponseModel.Pageviews
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i * 4
            });
            sessionsList.Add(new PageViewsResponseModel.Sessions
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i * 8
            });
        }

        var pageViewResponse = new PageViewsResponseModel
        {
            pageviews = pageViewsList.ToArray(),
            sessions = sessionsList.ToArray()
        };
        var json = JsonSerializer.Serialize(pageViewResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage ReturnAuthenticatedMessage()
    {
        var authResponse = new AuthResponse
        {
            Token = "1234567890",
            User = new UserResponse
            {
                Id = "123",
                Username = "test",
                Role = "admin",
                CreatedAt = DateTime.Now,
                IsAdmin = true
            }
        };
        var json = JsonSerializer.Serialize(authResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private record AuthRequest(string username, string password);
}