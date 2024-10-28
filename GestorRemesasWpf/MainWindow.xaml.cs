using GestorRemesasWpf.ViewModels;
using LibUtil;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestorRemesasWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> mutuas;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ExpedienteViewModel(ExpedientesDataGrid);
            LoadMutuas();
        }


        private void LoadMutuas()
        {
            try
            {
                var iniFile = new IniFile("GestorRemesasWpf.ini");
                string mutuasPath = iniFile.ReadValue("Mutuas", "Path");

                if (File.Exists(mutuasPath))
                {
                    mutuas = File.ReadAllLines(mutuasPath)
                                 .Where(line => !string.IsNullOrWhiteSpace(line))
                                 .OrderBy(m => m)
                                 .ToList();
                    cmbMutuas.ItemsSource = mutuas;
                }
                else
                {
                    MessageBox.Show($"El archivo de mutuas no se encontró en la ruta: {mutuasPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las mutuas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}