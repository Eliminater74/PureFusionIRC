using PureFusionIRC.Core.Text;

namespace PureFusionIRC.Core.Tests;

public class ControlCodesTests
{
    [Fact]
    public void Strip_removes_mirc_attributes()
    {
        var raw = "\u0002bold\u000f \u000304red\u000f plain";
        Assert.Equal("bold red plain", ControlCodes.Strip(raw));
    }

    [Fact]
    public void Parse_tracks_bold_and_color()
    {
        var spans = ControlCodes.Parse("\u0002hi\u0003" + "04x\u000f!");
        Assert.Contains(spans, s => s.Style.Bold && s.Text == "hi");
        Assert.Contains(spans, s => s.Style.Foreground == 4 && s.Text == "x");
        Assert.Contains(spans, s => s.Text == "!" && !s.Style.Bold);
    }
}
