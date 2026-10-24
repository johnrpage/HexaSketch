using System.Globalization;
using System.Windows.Data;
using HexaSketch.Domain;

namespace HexaSketch.Wpf;

/// <summary>Shows an element type the way people say it: "primary actor", not "PrimaryActor".</summary>
public sealed class ElementTypeText : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ElementType type ? MainViewModel.TypeText(type) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
