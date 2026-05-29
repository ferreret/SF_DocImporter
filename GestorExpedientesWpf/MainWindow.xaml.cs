using GestorExpedientesWpf.Models;
using GestorExpedientesWpf.ViewModels;
using LibUtil;
using System.ComponentModel;
using System.IO;
using System.Reflection;
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
        private IniFile? _uiIni;
        private GridLength _visorDefaultWidth;
        private double _visorDefaultMinWidth;

        public MainWindow()
        {
            InitializeComponent();
            var vm = new ExpedientesViewModel();
            DataContext = vm;

            _visorDefaultWidth = VisorColumn.Width;
            _visorDefaultMinWidth = VisorColumn.MinWidth;

            LoadMutuas();
            LoadUiSettings(vm);

            vm.PropertyChanged += Vm_PropertyChanged;
            ApplyVisorColapsado(vm.IsVisorColapsado);

            Closing += MainWindow_Closing;

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"{Title} v{v?.Major}.{v?.Minor}.{v?.Build}";
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExpedientesViewModel.IsVisorColapsado)
                && DataContext is ExpedientesViewModel vm)
            {
                ApplyVisorColapsado(vm.IsVisorColapsado);
            }
        }

        private void ApplyVisorColapsado(bool colapsado)
        {
            if (colapsado)
            {
                VisorColumn.MinWidth = 0;
                VisorColumn.Width = new GridLength(0);
            }
            else
            {
                VisorColumn.MinWidth = _visorDefaultMinWidth;
                VisorColumn.Width = _visorDefaultWidth;
            }
        }

        private void LoadUiSettings(ExpedientesViewModel vm)
        {
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestorExpedientesWpf.ini");
                if (!File.Exists(path)) return;
                _uiIni = new IniFile(path);
                vm.UiScale = _uiIni.TryReadDouble("UI", "Zoom", 1.0);
                vm.IsVisorColapsado = _uiIni.TryReadBool("UI", "VisorColapsado", false);
            }
            catch
            {
                // Si falla, seguimos con los valores por defecto del VM
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                if (_uiIni != null && DataContext is ExpedientesViewModel vm)
                {
                    _uiIni.WriteDouble("UI", "Zoom", vm.UiScale);
                    _uiIni.WriteBool("UI", "VisorColapsado", vm.IsVisorColapsado);
                    _uiIni.Save();
                }
            }
            catch
            {
                // No bloquear el cierre por errores al guardar prefs UI
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            if (Keyboard.Modifiers == ModifierKeys.Control && DataContext is ExpedientesViewModel vm)
            {
                vm.UiScale += e.Delta > 0 ? ExpedientesViewModel.UiScaleStep : -ExpedientesViewModel.UiScaleStep;
                e.Handled = true;
            }
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}