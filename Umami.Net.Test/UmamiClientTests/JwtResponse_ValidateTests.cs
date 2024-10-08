﻿using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Models;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test.UmamiClientTests;

public class JwtResponse_ValidateTests
{
    private UmamiClient GetServices(HttpMessageHandler handler)
    {
        var services = SetupExtensions.SetupServiceCollection(handler: handler);
        SetupExtensions.SetupUmamiClient(services);
        var serviceProvider = services.BuildServiceProvider();
        var umamiClient = serviceProvider.GetRequiredService<UmamiClient>();
        return umamiClient;
    }

    [Fact]
    public async Task TrackPageView_WithBot()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UserAgent = "BOT" });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.BotDetected, response.Status);
    }

    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }

    [Fact]
    public async Task Identify_WithSessionId()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.IdentifyAndDecode("1234");
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }

    [Fact]
    public async Task Identify_Empty()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.IdentifyAndDecode();
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }

    [Fact]
    public async Task Track_NamedEvent()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackAndDecode("RSS");
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }

    [Fact]
    public async Task Track_EventObject()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackAndDecode(new UmamiPayload { Name = "RSS" });
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }

    [Fact]
    public async Task Track_EventObjectWithData()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackAndDecode(new UmamiPayload { Name = "RSS" },
            new UmamiEventData { { "key", "value" } });
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }

    [Fact]
    public async Task Identify_SessionAndDecode()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.IdentifySessionAndDecode("1234");
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }


    [Fact]
    public async Task Identify_SendAndDecode()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.SendAndDecode(new UmamiPayload { Name = "RSS" },
            new UmamiEventData { { "key", "value" } });
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }
}