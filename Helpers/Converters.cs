using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Convierte bool a Brush verde/rojo.
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
            {
                return activo
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                    : new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
            return new SolidColorBrush(Colors.Gray);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convierte bool a texto "✓ Activo" / "✕ Inactivo".
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
                return activo ? "✓ Activo" : "✕ Inactivo";
            return "Desconocido";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convierte string no vacío en Visible, vacío/null en Collapsed.
    /// Usado para el botón "✕ Limpiar" en barras de búsqueda.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convierte bool en Visible (true) o Collapsed (false).
    /// Usado para el indicador de carga en Reportes.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }

    /// <summary>
    /// Convierte un porcentaje (0-100) a un ancho en píxeles.
    /// Se usa con un parámetro que indica el ancho máximo.
    /// Ejemplo: {Binding Porcentaje, Converter={StaticResource PorcentajeAAncho}, ConverterParameter=300}
    /// </summary>
    public class PorcentajeAAnchoCon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double porcentaje = 0;
            double anchoMax = 300;

            if (value is double d) porcentaje = d;
            else if (value is int i) porcentaje = i;
            else if (value is decimal dec) porcentaje = (double)dec;

            if (parameter is string s && double.TryParse(s, out double p))
                anchoMax = p;

            porcentaje = Math.Max(0, Math.Min(100, porcentaje));
            return porcentaje / 100.0 * anchoMax;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// IMultiValueConverter: convierte (porcentaje 0-100, anchoContenedor) → anchoPixeles.
    /// Usado para la barra de progreso de tasa de asistencia en Reportes.
    /// </summary>
    public class PorcentajeAAnchoCon : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.0;
            if (values[0] is not decimal porcentaje) return 0.0;
            if (values[1] is not double ancho) return 0.0;

            // Proteger contra NaN y valores fuera de rango
            if (double.IsNaN(ancho) || ancho <= 0) return 0.0;
            porcentaje = Math.Max(0, Math.Min(100, porcentaje));

            return (double)porcentaje / 100.0 * ancho;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
