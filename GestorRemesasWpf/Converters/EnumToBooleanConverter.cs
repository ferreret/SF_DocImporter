using System;
using System.Globalization;
using System.Windows.Data;

namespace GestorRemesasWpf.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return false;

            string parameterString = parameter.ToString();
            if (string.IsNullOrEmpty(parameterString))
                return false;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return false;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return Binding.DoNothing;

            string parameterString = parameter.ToString();
            if (string.IsNullOrEmpty(parameterString))
                return Binding.DoNothing;

            return Enum.Parse(targetType, parameterString);
        }
    }
}
