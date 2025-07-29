using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CalculatorApp
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d.ToString("F1", CultureInfo.CurrentCulture);
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                // Удаляем лишние пробелы
                s = s.Trim();

                // Пробуем разные варианты парсинга как в старом рабочем коде
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out var result1))
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно преобразовано '{s}' в {result1} (текущая культура)");
                    return result1;
                }

                if (double.TryParse(s.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно преобразовано '{s}' в {result2} (инвариантная культура)");
                    return result2;
                }

                if (double.TryParse(s.Replace(".", ","), NumberStyles.Float, CultureInfo.CurrentCulture, out var result3))
                {
                    System.Diagnostics.Debug.WriteLine($"Успешно преобразовано '{s}' в {result3} (замена точки на запятую)");
                    return result3;
                }

                System.Diagnostics.Debug.WriteLine($"Не удалось преобразовать '{s}' в double");
            }
            return value;
        }
    }
}
