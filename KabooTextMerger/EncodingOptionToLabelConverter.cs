using System.Globalization;
using System.Windows.Data;
using KabooTextMerger.Models;
using KabooTextMerger.Services;

namespace KabooTextMerger;

public sealed class EncodingOptionToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TextEncodingOption option
            ? EncodingOptionMapper.ToLabel(option)
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

