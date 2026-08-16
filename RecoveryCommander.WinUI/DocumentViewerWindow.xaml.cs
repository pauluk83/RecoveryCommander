using Microsoft.UI.Xaml;

namespace RecoveryCommanderWinUI;

public sealed partial class DocumentViewerWindow : Window
{
    public DocumentViewerWindow()
    {
        this.InitializeComponent();
    }

    public DocumentViewerWindow(string title, string content) : this()
    {
        TitleTextBlock.Text = title;
        DocumentTextBox.Text = content;
    }
}


