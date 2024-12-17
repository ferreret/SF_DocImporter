using GestorExpedientesWpf.Models;
using GestorExpedientesWpf.ViewModels;
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
        public MainWindow()
        {            
            InitializeComponent();                        
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
    }
}