using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umami.Net.Config;
using Umami.Net.Models;

namespace Umami.Net.Test.Helpers;

/// <summary>
/// Tests for PayloadService to ensure correct payload population and data handling.
/// </summary>
public class PayloadService_Tests
{
    private readonly UmamiClientSettings _settings = new()
    {
        UmamiPath = "https://analytics.test.com",
        WebsiteId = "12345678-1234-1234-1234-123456789abc"
    };

    /// <summary>
    /// Creates a PayloadService with mocked dependencies for testing.
    /// </summary>
    private PayloadService CreatePayloadService(IHttpContextAccessor? httpContextAccessor = null)
    {
        httpContextAccessor ??= CreateMockHttpContextAccessor();
        return new PayloadService(httpContextAccessor, _settings, NullLogger<PayloadService>.Instance);
    }

    /// <summary>
    /// Creates a mock IHttpContextAccessor with default test values.
    /// </summary>
    private static IHttpContextAccessor CreateMockHttpContextAccessor()
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();
        var mockContext = new DefaultHttpContext();

        mockContext.Request.Headers.UserAgent = "Mozilla/5.0 Test";
        mockContext.Request.Path = "/test/path";
        mockContext.Request.Host = new HostString("test.example.com");
        mockContext.Request.Headers.Referer = "https://referrer.com";
        mockContext.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";

        mockAccessor.Setup(a => a.HttpContext).Returns(mockContext);
        return mockAccessor.Object;
    }

    /// <summary>
    /// Verifies that PopulateFromPayload creates new payload when null is provided.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_NullPayload_CreatesNewPayload()
    {
        // Arrange
        var service = CreatePayloadService();

        // Act
        var result = service.PopulateFromPayload(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_settings.WebsiteId, result.Website);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload preserves existing payload data.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_ExistingPayload_PreservesData()
    {
        // Arrange
        var service = CreatePayloadService();
        var existingPayload = new UmamiPayload
        {
            Name = "test-event",
            Url = "/custom/url",
            Title = "Custom Title"
        };

        // Act
        var result = service.PopulateFromPayload(existingPayload, null);

        // Assert
        Assert.Equal("test-event", result.Name);
        Assert.Equal("/custom/url", result.Url);
        Assert.Equal("Custom Title", result.Title);
        Assert.Equal(_settings.WebsiteId, result.Website);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload merges event data correctly.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_WithEventData_MergesData()
    {
        // Arrange
        var service = CreatePayloadService();
        var eventData = new UmamiEventData
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var result = service.PopulateFromPayload(null, eventData);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("value1", result.Data["key1"]);
        Assert.Equal("value2", result.Data["key2"]);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload auto-detects URL from HttpContext.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_NoUrl_UsesHttpContext()
    {
        // Arrange
        var service = CreatePayloadService();

        // Act
        var result = service.PopulateFromPayload(null, null);

        // Assert
        Assert.Equal("/test/path", result.Url);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload auto-detects hostname from HttpContext.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_NoHostname_UsesHttpContext()
    {
        // Arrange
        var service = CreatePayloadService();

        // Act
        var result = service.PopulateFromPayload(null, null);

        // Assert
        Assert.Equal("test.example.com", result.Hostname);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload auto-detects referrer from HttpContext.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_NoReferrer_UsesHttpContext()
    {
        // Arrange
        var service = CreatePayloadService();

        // Act
        var result = service.PopulateFromPayload(null, null);

        // Assert
        Assert.Equal("https://referrer.com", result.Referrer);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload returns null language when not provided.
    /// Language is not auto-detected from HttpContext in current implementation.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_NoLanguage_ReturnsNull()
    {
        // Arrange
        var service = CreatePayloadService();

        // Act
        var result = service.PopulateFromPayload(null, null);

        // Assert
        Assert.Null(result.Language);
    }

    /// <summary>
    /// Verifies that UseDefaultUserAgent flag stores the user agent (from payload or default) in Data.
    /// If payload.UserAgent is null, DefaultUserAgent is used and stored.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_UseDefaultUserAgent_StoresAgent()
    {
        // Arrange
        var service = CreatePayloadService();
        var payload = new UmamiPayload { UseDefaultUserAgent = true };

        // Act
        var result = service.PopulateFromPayload(payload, null);

        // Preserve the inbound agent before applying the explicit override.
        Assert.NotNull(result.Data);
        Assert.Contains("OriginalUserAgent", result.Data.Keys);
        Assert.Equal(PayloadService.DefaultUserAgent, result.UserAgent);
        Assert.Equal("Mozilla/5.0 Test", result.Data["OriginalUserAgent"]);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload handles null HttpContext gracefully.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_NullHttpContext_DoesNotThrow()
    {
        // Arrange
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var service = new PayloadService(mockAccessor.Object, _settings, NullLogger<PayloadService>.Instance);

        // Act
        var result = service.PopulateFromPayload(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_settings.WebsiteId, result.Website);
    }

    /// <summary>
    /// Verifies that DefaultUserAgent constant is set correctly.
    /// </summary>
    [Fact]
    public void DefaultUserAgent_IsSet()
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(PayloadService.DefaultUserAgent));
        Assert.Contains("Umami", PayloadService.DefaultUserAgent);
    }

    /// <summary>
    /// Verifies that PopulateFromPayload preserves custom values over auto-detection.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_CustomValues_OverrideAutoDetection()
    {
        // Arrange
        var service = CreatePayloadService();
        var customPayload = new UmamiPayload
        {
            Url = "/custom/url",
            Hostname = "custom.com",
            Referrer = "https://custom-referrer.com",
            Language = "fr-FR"
        };

        // Act
        var result = service.PopulateFromPayload(customPayload, null);

        // Assert
        Assert.Equal("/custom/url", result.Url);
        Assert.Equal("custom.com", result.Hostname);
        Assert.Equal("https://custom-referrer.com", result.Referrer);
        Assert.Equal("fr-FR", result.Language);
    }

    /// <summary>
    /// Verifies that payload.Data takes precedence over eventData parameter.
    /// When both are provided, payload.Data is used.
    /// </summary>
    [Fact]
    public void PopulateFromPayload_ExistingData_PayloadDataTakesPrecedence()
    {
        // Arrange
        var service = CreatePayloadService();
        var payload = new UmamiPayload
        {
            Data = new UmamiEventData { { "existing", "value" } }
        };
        var newData = new UmamiEventData { { "new", "data" } };

        // Act
        var result = service.PopulateFromPayload(payload, newData);

        // Assert - payload.Data takes precedence
        Assert.NotNull(result.Data);
        Assert.Equal("value", result.Data["existing"]);
        Assert.False(result.Data.ContainsKey("new")); // newData is not merged
    }
}
