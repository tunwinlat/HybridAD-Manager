using HybridADManager.Models;
using System.Globalization;
using System.Windows.Data;

namespace HybridADManager.Infrastructure.Converters;

public class ObjectTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var type = value switch
        {
            NodeType nt => nt,
            string s => Enum.TryParse<NodeType>(s, out var parsed) ? parsed : NodeType.Domain,
            _ => NodeType.Domain
        };

        return type switch
        {
            NodeType.Domain => "/Resources/domain.png",
            NodeType.BuiltinContainer or NodeType.UsersContainer or NodeType.ComputersContainer or
            NodeType.DomainControllersContainer or NodeType.ForeignSecurityPrincipalsContainer or
            NodeType.ManagedServiceAccountsContainer => "/Resources/container.png",
            NodeType.CustomOU => "/Resources/ou.png",
            NodeType.SavedQueries => "/Resources/query.png",
            _ => "/Resources/domain.png"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
