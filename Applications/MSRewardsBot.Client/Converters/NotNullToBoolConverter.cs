using System;
using System.Globalization;
using System.Windows.Data;

namespace MSRewardsBot.Client.Converters
{
    /// <summary>
    /// Converts a reference value into a bool: true if not null, false otherwise.
    /// Used by XAML DataTriggers that need to react to "value is set" without binding to a specific property.
    /// </summary>
    public sealed class NotNullToBoolConverter : IValueConverter
    {
        public static readonly NotNullToBoolConverter Instance = new NotNullToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
