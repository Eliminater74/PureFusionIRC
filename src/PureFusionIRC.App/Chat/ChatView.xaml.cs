using System.Windows.Controls;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.App.Chat;

public partial class ChatView : UserControl
{
    private IrcBuffer? _buffer;
    private ThemeDefinition _theme = BuiltInThemes.AmoledBlack;
    private AppSettings _settings = new();

    public ChatView()
    {
        InitializeComponent();
    }

    public void Configure(ThemeDefinition theme, AppSettings settings)
    {
        _theme = theme;
        _settings = settings;
        Document.FontFamily = new System.Windows.Media.FontFamily(settings.FontFamily);
        Document.FontSize = settings.FontSize;
        if (_buffer is not null)
        {
            Show(_buffer);
        }
    }

    public void Show(IrcBuffer buffer)
    {
        _buffer = buffer;
        Document.Blocks.Clear();
        foreach (var line in buffer.Lines)
        {
            Document.Blocks.Add(ChatDocumentBuilder.Build(line, _theme, _settings));
        }

        ScrollToEnd();
        buffer.Activity = BufferActivity.None;
    }

    public void Append(IrcBuffer buffer, ChatLine line)
    {
        if (!ReferenceEquals(buffer, _buffer))
        {
            return;
        }

        Document.Blocks.Add(ChatDocumentBuilder.Build(line, _theme, _settings));
        while (Document.Blocks.Count > _settings.MaxBufferLines)
        {
            Document.Blocks.Remove(Document.Blocks.FirstBlock);
        }

        ScrollToEnd();
        buffer.Activity = BufferActivity.None;
    }

    public void Clear()
    {
        Document.Blocks.Clear();
        _buffer?.Clear();
    }

    private void ScrollToEnd()
    {
        ChatBox.ScrollToEnd();
    }
}
