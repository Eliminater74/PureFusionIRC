using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Text;

namespace PureFusionIRC.App.Tray;

/// <summary>NotifyIcon host: restore, balloon tips, and an Exit that really quits.</summary>
public sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon _ownedIcon;

    public TrayController(Action restore, Action openNetworks, Action exit)
    {
        _ownedIcon = ExtractIcon();
        _icon = new NotifyIcon
        {
            Text = "PureFusionIRC",
            Visible = true,
            Icon = _ownedIcon
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open PureFusionIRC", null, (_, _) => restore());
        menu.Items.Add("Networks…", null, (_, _) =>
        {
            restore();
            openNetworks();
        });
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => exit());
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => restore();
        _icon.BalloonTipClicked += (_, _) => restore();
    }

    public void Notify(string title, string body, bool windowIsInForeground)
    {
        if (windowIsInForeground || string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        var text = ControlCodes.Strip(body);
        if (text.Length > 120)
        {
            text = text[..117] + "…";
        }

        _icon.ShowBalloonTip(4500, title, text, ToolTipIcon.Info);
    }

    public void NotifyChat(IrcBuffer buffer, ChatLine line, bool windowIsInForeground)
    {
        if (line.IsSelf)
        {
            return;
        }

        if (line.IsHighlight)
        {
            Notify($"Highlight in {buffer.Name}", $"{line.Nick}: {line.Text}", windowIsInForeground);
            return;
        }

        if (buffer.Kind == BufferKind.Query &&
            line.Kind is ChatLineKind.Message or ChatLineKind.Action or ChatLineKind.Notice)
        {
            Notify("Private message", $"{line.Nick}: {line.Text}", windowIsInForeground);
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _ownedIcon.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Icon ExtractIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/PureFusionIRC.ico"));
        if (resource?.Stream is { } packed)
        {
            using (packed)
            {
                using var copy = new MemoryStream();
                packed.CopyTo(copy);
                copy.Position = 0;
                using var fromPack = new Icon(copy);
                return (Icon)fromPack.Clone();
            }
        }

        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path))
        {
            var associated = Icon.ExtractAssociatedIcon(path);
            if (associated is not null)
            {
                return associated;
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
