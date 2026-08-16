using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace RecoveryCommanderWinUI.Dialogs
{
    public sealed partial class CrashDialog : ContentDialog
    {
        public CrashDialog(Exception exception)
        {
            this.InitializeComponent();
            ErrorMessageTextBlock.Text = exception.Message;
            StackTraceTextBlock.Text = exception.StackTrace ?? "No stack trace available";
            
            PrimaryButtonClick += (s, e) =>
            {
                var errorInfo = $"Error: {exception.Message}\n\nStack Trace:\n{exception.StackTrace}";
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(errorInfo);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            };
        }
    }
}


