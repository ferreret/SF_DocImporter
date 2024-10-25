using GestorRemesasWpf.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GestorRemesasWpf.Converters
{
    public class UniqueCoberturaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var expedientes = value as IEnumerable<Expediente>;
            var coberturas = expedientes?.Select(e => e.Cobertura).Distinct().ToList();
            coberturas?.Insert(0, ""); // Añadir una opción vacía para mostrar todos
            return coberturas;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
