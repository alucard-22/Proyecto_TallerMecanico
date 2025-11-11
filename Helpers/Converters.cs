using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Convierte bool a color (Verde/Rojo)
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
            {
                return activo ?
                    new SolidColorBrush(Color.FromRgb(39, 174, 96)) :  // Verde
                    new SolidColorBrush(Color.FromRgb(231, 76, 60));   // Rojo
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte bool a texto (Activo/Inactivo)
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
            {
                return activo ? "✓ Activo" : "✕ Inactivo";
            }
            return "Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
