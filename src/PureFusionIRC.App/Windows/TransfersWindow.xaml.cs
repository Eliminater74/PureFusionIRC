using System.Diagnostics;
using System.IO;
using System.Windows;
using PureFusionIRC.Core;
using PureFusionIRC.Core.Dcc;

namespace PureFusionIRC.App.Windows;

public partial class TransfersWindow : Window
{
    private readonly ClientRuntime _runtime;

    public event Action? SendFileRequested;

    public TransfersWindow(ClientRuntime runtime)
    {
        _runtime = runtime;
        InitializeComponent();
        List.ItemsSource = runtime.Dcc.Transfers;
    }

    private void SaveItem_Click(object sender, RoutedEventArgs e)
    {
        if (Item(sender) is { CanAccept: true } transfer)
        {
            _runtime.Dcc.Accept(transfer);
        }
    }

    private void DeclineItem_Click(object sender, RoutedEventArgs e)
    {
        if (Item(sender) is { } transfer)
        {
            _runtime.Dcc.Decline(transfer);
        }
    }

    private void CancelItem_Click(object sender, RoutedEventArgs e)
    {
        if (Item(sender) is { } transfer)
        {
            _runtime.Dcc.Cancel(transfer);
        }
    }

    private void OpenItem_Click(object sender, RoutedEventArgs e)
    {
        if (Item(sender) is not { } transfer || string.IsNullOrEmpty(transfer.FilePath))
        {
            return;
        }

        var path = transfer.Status == DccStatus.Completed && File.Exists(transfer.FilePath)
            ? transfer.FilePath
            : Path.GetDirectoryName(transfer.FilePath);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = Directory.Exists(path) ? path : "/select,\"" + path + "\"",
            UseShellExecute = true
        });
    }

    private void Send_Click(object sender, RoutedEventArgs e) => SendFileRequested?.Invoke();

    private void Folder_Click(object sender, RoutedEventArgs e)
    {
        var folder = _runtime.Dcc.FolderFor(_runtime.Document.App);
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }

    private static DccTransfer? Item(object sender) =>
        (sender as FrameworkElement)?.Tag as DccTransfer;
}
