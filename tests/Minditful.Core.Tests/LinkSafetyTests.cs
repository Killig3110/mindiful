using Minditful.Integrations.Live;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>Link trong lời mời họp / email do người khác gửi: chỉ mở trang web hoặc Teams, không bao giờ mở file hay chương trình trên máy.</summary>
public class LinkSafetyTests
{
    [Theory]
    [InlineData("https://teams.microsoft.com/l/meetup-join/abc", true)]
    [InlineData("https://outlook.office365.com/owa/?itemid=AAMk%2F", true)]
    [InlineData("msteams:/l/meetup-join/abc", true)]
    [InlineData("file:///C:/Windows/System32/calc.exe", false)]
    [InlineData(@"C:\Windows\System32\calc.exe", false)]
    [InlineData(@"\\evil\share\x.exe", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("ms-settings:", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_web_and_teams_links_are_opened(string? url, bool ok) => Assert.Equal(ok, LiveActionSink.IsSafeLink(url));
}
