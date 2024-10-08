using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil
{
    public static class StringExtensions
    {
        /// <summary>
        /// Extensión para eliminar los retornos de carro y saltos de línea de una cadena.
        /// </summary>
        /// <param name="input">La cadena de texto a procesar.</param>
        /// <returns>La cadena sin retornos de carro ni saltos de línea.</returns>
        public static string RemoveCarriageReturns(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remueve los caracteres de retorno de carro (\r) y de nueva línea (\n)
            return input.Replace("\r", "").Replace("\n", " ");
        }
    }
}
