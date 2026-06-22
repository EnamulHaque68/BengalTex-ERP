using BengalTex.ERP.Application.Attendance;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Application.Tests.Attendance;

public class UserAgentParserTests
{
    [Fact]
    public void Parse_AndroidChromeMobile()
    {
        var ua = "Mozilla/5.0 (Linux; Android 13; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().Be("Mobile");
        d.Os.Should().Be("Android");
        d.Browser.Should().Be("Chrome");
    }

    [Fact]
    public void Parse_iPhoneSafari()
    {
        var ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1";
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().Be("Mobile");
        d.Os.Should().Be("iOS");
        d.Browser.Should().Be("Safari");
    }

    [Fact]
    public void Parse_iPadIsTablet()
    {
        var ua = "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().Be("Tablet");
        d.Os.Should().Be("iOS");
    }

    [Fact]
    public void Parse_WindowsEdgeDesktop()
    {
        var ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0";
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().Be("Desktop");
        d.Os.Should().Be("Windows");
        d.Browser.Should().Be("Edge");   // Edge must win over Chrome/Safari substrings
    }

    [Fact]
    public void Parse_MacFirefoxDesktop()
    {
        var ua = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0";
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().Be("Desktop");
        d.Os.Should().Be("macOS");
        d.Browser.Should().Be("Firefox");
    }

    [Fact]
    public void Parse_AndroidTabletNoMobileToken()
    {
        var ua = "Mozilla/5.0 (Linux; Android 12; SM-X200) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().Be("Tablet");   // Android without "Mobile" => tablet
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyReturnsNulls(string? ua)
    {
        var d = UserAgentParser.Parse(ua);
        d.DeviceType.Should().BeNull();
        d.Browser.Should().BeNull();
        d.Os.Should().BeNull();
    }
}
