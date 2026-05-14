using System.Windows;

namespace HybridADManager.Infrastructure.Themes;

public static class ThemeManager
{
    public static void ApplyHighContrast()
    {
        var app = Application.Current;
        if (app == null) return;

        // Remove existing Colors.xaml if present
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Colors.xaml") == true);
        if (existing != null)
            app.Resources.MergedDictionaries.Remove(existing);

        // Add high-contrast colors
        app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Infrastructure/Themes/HighContrastColors.xaml")
        });
    }

    public static void ApplyDefaultTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        var hc = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("HighContrastColors.xaml") == true);
        if (hc != null)
            app.Resources.MergedDictionaries.Remove(hc);

        app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Infrastructure/Themes/Colors.xaml")
        });
    }
}
