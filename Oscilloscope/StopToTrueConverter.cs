using System.Globalization;
using System.Windows.Data;

namespace Oscilloscope;

[ValueConversion(typeof(OscilloscopeStatus), typeof(bool))]
internal sealed class StopToTrueConverter : IValueConverter
{
    public static StopToTrueConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is OscilloscopeStatus.Stop;

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) => throw new NotImplementedException();
}
