using System.Windows;

namespace HybridADManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void HighContrast_Checked(object sender, RoutedEventArgs e)
    {
        Infrastructure.Themes.ThemeManager.ApplyHighContrast();
    }

    private void HighContrast_Unchecked(object sender, RoutedEventArgs e)
    {
        Infrastructure.Themes.ThemeManager.ApplyDefaultTheme();
    }
}
