using System.Windows;
using PureFusionIRC.Core.Updates;

namespace PureFusionIRC.App.Windows;

public partial class ChangelogWindow : Window
{
    public ChangelogWindow(string version)
    {
        InitializeComponent();
        VersionBlock.Text = "PureFusionIRC " + version;
        BodyBox.Text = ChangelogText.LoadEmbedded();
    }

    public bool OpenUpdates { get; private set; }

    private void Updates_Click(object sender, RoutedEventArgs e)
    {
        OpenUpdates = true;
        DialogResult = true;
        Close();
    }
}
