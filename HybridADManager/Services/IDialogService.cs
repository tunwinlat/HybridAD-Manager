namespace HybridADManager.Services;

public interface IDialogService
{
    void ShowInfo(string message, string title = "Information");
    void ShowWarning(string message, string title = "Warning");
    void ShowError(string message, string title = "Error");
    bool AskYesNo(string message, string title = "Confirm");
    string? ShowOpenFileDialog(string filter, string title = "Open File");
    string? ShowSaveFileDialog(string filter, string defaultFileName, string title = "Save File");
}
