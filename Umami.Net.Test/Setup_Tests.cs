using Umami.Net.Config;

namespace Umami.Net.Test;

public class Setup_Tests
{
    [Fact]
    public void Validate_WebsiteId_Guid()
    {
        var websiteId = "E289FB9D-EF7B-435F-B0CC-FBCD1BEC6E49";
        var umamiClient = new UmamiClientSettings();
        umamiClient.UmamiPath = "http://test.com";
        umamiClient.WebsiteId = websiteId;
        Setup.ValidateSettings(umamiClient);
    }

    [Fact]
    public void Validate_WebsiteId_NotGuid()
    {
        var websiteId = "websiteId";
        var umamiClient = new UmamiClientSettings();
        umamiClient.UmamiPath = "http://test.com";
        umamiClient.WebsiteId = websiteId;
        Assert.Throws<FormatException>(() => Setup.ValidateSettings(umamiClient));
    }

    [Fact]
    public void Validate_WebsitePath_NotNull()
    {
        var umamiClient = new UmamiClientSettings();
        umamiClient.UmamiPath = "http://test.com";
        umamiClient.WebsiteId = "";
        Assert.Throws<ArgumentNullException>(() => Setup.ValidateSettings(umamiClient));
    }

    [Fact]
    public void Validate_WebsitePath_NotUrl()
    {
        var umamiClient = new UmamiClientSettings();
        umamiClient.UmamiPath = "boop";
        umamiClient.WebsiteId = "";
        Assert.Throws<FormatException>(() => Setup.ValidateSettings(umamiClient));
    }

    [Fact]
    public void Validate_WebsitePath_Url()
    {
        var umamiClient = new UmamiClientSettings();
        umamiClient.UmamiPath = "http://test.com";
        umamiClient.WebsiteId = "E289FB9D-EF7B-435F-B0CC-FBCD1BEC6E49";
        Setup.ValidateSettings(umamiClient);
    }
}