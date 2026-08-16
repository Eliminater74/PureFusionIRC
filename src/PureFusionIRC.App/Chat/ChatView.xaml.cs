using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PureFusionIRC.Core.Buffers;
using PureFusionIRC.Core.Models;
using PureFusionIRC.Core.Text;
using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.App.Chat;

public partial class ChatView : UserControl
{
    private IrcBuffer? _buffer;
    private ThemeDefinition _theme = BuiltInThemes.AmoledBlack;
    private AppSettings _settings = new();
    private ChatLine? _menuLine;
    private Uri? _menuUri;

    public event Action<ChatLine>? ReplyLine;
    public event Action<ChatLine>? ReactLine;
    public event Action<string>? QueryNick;
    public event Action<string>? WhoisNick;

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

    private void ChatMenu_Opened(object sender, RoutedEventArgs e)
    {
        ResolveMenuTarget();
        var nick = _menuLine?.Nick;
        var hasNick = !string.IsNullOrEmpty(nick);
        ReplyItem.IsEnabled = hasNick;
        ReactItem.IsEnabled = hasNick && _menuLine?.Kind is ChatLineKind.Message or ChatLineKind.Action;
        QueryItem.IsEnabled = hasNick;
        WhoisItem.IsEnabled = hasNick;
        CopyNickItem.IsEnabled = hasNick;
        OpenLinkItem.Visibility = _menuUri is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ResolveMenuTarget()
    {
        _menuLine = null;
        _menuUri = null;
        var point = Mouse.GetPosition(ChatBox);
        var pointer = ChatBox.GetPositionFromPoint(point, true) ?? ChatBox.CaretPosition;
        for (DependencyObject? node = pointer?.Parent as DependencyObject;
             node is not null;
             node = LogicalTreeHelper.GetParent(node) ?? VisualTreeHelper.GetParent(node))
        {
            if (node is Hyperlink link)
            {
                _menuUri = link.Tag as Uri;
            }

            if (node is Paragraph paragraph)
            {
                _menuLine = paragraph.Tag as ChatLine;
                break;
            }
        }
    }

    private void Reply_Click(object sender, RoutedEventArgs e)
    {
        if (_menuLine is not null && !string.IsNullOrEmpty(_menuLine.Nick))
        {
            ReplyLine?.Invoke(_menuLine);
        }
    }

    private void React_Click(object sender, RoutedEventArgs e)
    {
        if (_menuLine is not null)
        {
            ReactLine?.Invoke(_menuLine);
        }
    }

    private void Query_Click(object sender, RoutedEventArgs e)
    {
        if (_menuLine?.Nick is { Length: > 0 } nick)
        {
            QueryNick?.Invoke(nick);
        }
    }

    private void Whois_Click(object sender, RoutedEventArgs e)
    {
        if (_menuLine?.Nick is { Length: > 0 } nick)
        {
            WhoisNick?.Invoke(nick);
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!ChatBox.Selection.IsEmpty)
        {
            ChatBox.Copy();
            return;
        }

        if (_menuLine is not null)
        {
            var body = string.IsNullOrEmpty(_menuLine.Nick)
                ? _menuLine.Text
                : "<" + _menuLine.Nick + "> " + _menuLine.Text;
            Clipboard.SetText(body);
        }
    }

    private void CopyNick_Click(object sender, RoutedEventArgs e)
    {
        if (_menuLine?.Nick is { Length: > 0 } nick)
        {
            Clipboard.SetText(nick);
        }
    }

    private void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if (_menuUri is not null)
        {
            UrlMatcher.Open(_menuUri);
        }
    }
}
