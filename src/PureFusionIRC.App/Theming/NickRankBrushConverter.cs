using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PureFusionIRC.App.Theming;

public sealed class NickRankBrushConverter : IValueConverter, IMultiValueConverter
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

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is true)
        {
            return Application.Current?.TryFindResource("SelfNickBrush") as Brush
                   ?? Brushes.LightGreen;
        }

        return Convert(values.Length > 1 ? values[1] : null, targetType, parameter, culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
