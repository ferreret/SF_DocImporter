using GestorExpedientesWpf.Models;
using GestorExpedientesWpf.ViewModels;
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


namespace GestorExpedientesWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _mutuas;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ExpedientesViewModel();
            LoadMutuas();                 
        }

        private void LoadMutuas()
        {
            try
            {
                var iniFile = new IniFile("GestorExpedientesWpf.ini"); // Cambia el nombre del archivo .ini si es necesario
                string mutuasPath = iniFile.ReadValue("Mutuas", "Path");

                if (File.Exists(mutuasPath))
                {
                    _mutuas = File.ReadAllLines(mutuasPath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .OrderBy(m => m)
                        .ToList();
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

        private void ResultadosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
            
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Expediente selectedExpediente)
            {
                var viewModel = DataContext as ExpedientesViewModel;
                if (viewModel?.AddSeleccionCommand.CanExecute(selectedExpediente) == true)
                {
                    viewModel.AddSeleccionCommand.Execute(selectedExpediente);
                }
            }
        }

        private void SeleccionadosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Expediente selectedExpediente)
            {
                var viewModel = DataContext as ExpedientesViewModel;
                viewModel?.RemoveSeleccionCommand.Execute(selectedExpediente);
            }
        }

        private void txtMutuas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Down)
            {
                if (popupMutuas.IsOpen && lstMutuas.Items.Count > 0)
                {
                    lstMutuas.Focus();
                    lstMutuas.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
        }

        private void txtMutuas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            txtMutuas.SelectAll();
        }

        private void lstMutuas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SeleccionarMutua();
        }

        private void lstMutuas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SeleccionarMutua();
                e.Handled = true;
            }
        }

        private void SeleccionarMutua()
        {
            if (lstMutuas.SelectedItem != null)
            {
                txtMutuas.Text = lstMutuas.SelectedItem.ToString();
                popupMutuas.IsOpen = false;
            }
        }

        private void txtMutuas_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtMutuas.Text))
            {
                popupMutuas.IsOpen = false;
                return;
            }

            string textoBusqueda = txtMutuas.Text.ToLower();
            List<string> resultadosFiltrados = _mutuas
                .Where(mutua => mutua.ToLower().Contains(textoBusqueda))
                .ToList();

            lstMutuas.ItemsSource = resultadosFiltrados;
            popupMutuas.IsOpen = true;
        }
    }
}