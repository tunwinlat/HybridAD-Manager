using Microsoft.Win32;
using System.Windows;

namespace HybridADManager.Services;

public class DialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool AskYesNo(string message, string title = "Confirm")
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public string? ShowOpenFileDialog(string filter, string title = "Open File")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowSaveFileDialog(string filter, string defaultFileName, string title = "Save File")
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName,
            Title = title
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
