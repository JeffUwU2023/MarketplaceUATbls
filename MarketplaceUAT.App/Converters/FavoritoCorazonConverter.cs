using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MarketplaceUAT.App.Converters
{
    public class FavoritoCorazonConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is bool esFavorito && esFavorito)
                return "♥";

            return "♡";
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
