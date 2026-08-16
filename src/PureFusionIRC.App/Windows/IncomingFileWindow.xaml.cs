using System.Windows;
using PureFusionIRC.Core.Dcc;

namespace PureFusionIRC.App.Windows;

public partial class IncomingFileWindow : Window
{
    public IncomingFileWindow(DccTransfer transfer, string saveFolder)
    {
        InitializeComponent();
        Transfer = transfer;
        FileNameBlock.Text = transfer.FileName;
        MetaBlock.Text = transfer.PeerNick + "  ·  " + DccParser.FormatBytes(transfer.FileSize);
        HowBlock.Text = transfer.IsReverse
            ? "After Save we open a local port so they can connect in. No port forwarding is needed on their side. If nothing starts, a firewall on this PC may be blocking it."
            : "After Save we connect out to them. That is the usual case from a home network; you do not need to open a port.";
        SaveBlock.Text = "Saved files go to: " + saveFolder + "  (Tools → Options → Files)";
        if (transfer.IsRisky)
        {
            RiskBlock.Visibility = Visibility.Visible;
            RiskBlock.Text = "This looks like a program. Do not save it unless you asked for it.";
        }
    }

    public DccTransfer Transfer { get; }
    public bool SaveChosen { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveChosen = true;
        DialogResult = true;
        Close();
    }

    private void Decline_Click(object sender, RoutedEventArgs e)
    {
        SaveChosen = false;
        DialogResult = false;
        Close();
    }
}
