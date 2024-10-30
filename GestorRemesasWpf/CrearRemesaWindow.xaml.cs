using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestorRemesasWpf
{
    /// <summary>
    /// Lógica de interacción para CrearRemesaWindow.xaml
    /// </summary>
    public partial class CrearRemesaWindow : Window
    {
        public CrearRemesaWindow()
        {
            InitializeComponent();
        }

        private void Cancelar_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
