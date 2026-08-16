using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PureFusionIRC.App.Theming;

public sealed class NickRankBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is char prefix
            ? prefix switch
            {
                '~' => "NickOwnerBrush",
                '&' => "NickAdminBrush",
                '@' => "NickOpBrush",
                '%' => "NickHalfopBrush",
                '+' => "NickVoiceBrush",
                _ => "NickRegularBrush"
            }
            : "NickRegularBrush";

        return Application.Current?.TryFindResource(key) as Brush
               ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
