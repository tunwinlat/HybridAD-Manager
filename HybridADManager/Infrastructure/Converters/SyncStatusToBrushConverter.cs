using HybridADManager.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HybridADManager.Infrastructure.Converters;

public class SyncStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value switch
        {
            SyncState ss => ss,
            _ => SyncState.Unknown
        };

        return status switch
        {
            SyncState.InSync => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10")!),
            SyncState.Pending => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB900")!),
            SyncState.CloudOnly => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D7")!),
            SyncState.SyncError => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D83B01")!),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#606060")!)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
