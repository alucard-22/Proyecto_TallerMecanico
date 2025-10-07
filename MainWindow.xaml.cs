using System.Configuration;
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
using Proyecto_taller.Views;

//namespace Proyecto_taller
//{
//    /// <summary>
//    /// Interaction logic for MainWindow.xaml
//    /// </summary>
//    public partial class MainWindow : Window
//    {
//        public MainWindow()
//        {
//            InitializeComponent();
//            MainFrame.Navigate(new Inicio());
//        }
//        private void Inicio_Click(object sender, RoutedEventArgs e)
//        {
//            MainFrame.Navigate(new Inicio());
//        }

//        private void Clientes_Click(object sender, RoutedEventArgs e)
//        {
//            MainFrame.Navigate(new Clientes());
//        }

//        private void Servicios_Click(object sender, RoutedEventArgs e)
//        {
//            MainFrame.Navigate(new Servicios());
//        }

//        private void Reservas_Click(object sender, RoutedEventArgs e)
//        {
//            MainFrame.Navigate(new Reservas());
//        }

//        private void Facturacion_Click(object sender, RoutedEventArgs e)
//        {
//            MainFrame.Navigate(new Facturacion());
//        }

//        private void Configuracion_Click(object sender, RoutedEventArgs e)
//        {
//            MainFrame.Navigate(new Configuracion());
//        }
//    }
//}
namespace Proyecto_taller
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;

        public MainWindow()
        {
            InitializeComponent();
            SetActiveButton(BtnInicio);
            NavigateToPage("Inicio");
        }

        private void SetActiveButton(Button button)
        {
            // Resetear el estilo del botón activo anterior
            if (_activeButton != null)
            {
                _activeButton.Style = (Style)FindResource("MenuButton");
            }

            // Establecer el nuevo botón activo
            _activeButton = button;
            _activeButton.Style = (Style)FindResource("ActiveMenuButton");
        }

        private void NavigateToPage(string pageName)
        {
            TitleTextBlock.Text = pageName;

            switch (pageName.ToLower())
            {
                case "inicio":
                    MainFrame.Navigate(new Inicio());
                    break;
                case "clientes":
                    MainFrame.Navigate(new Clientes());
                    break;
                case "vehículos":
                    MainFrame.Navigate(new Vehiculos());
                    break;
                case "trabajos":
                    MainFrame.Navigate(new Trabajos());
                    break;
                case "reservas":
                    MainFrame.Navigate(new Reservas());
                    break;
                case "inventario":
                    MainFrame.Navigate(new Inventario());
                    break;
                case "facturación":
                    MainFrame.Navigate(new Facturacion());
                    break;
                case "reportes":
                    MainFrame.Navigate(new Reportes());
                    break;
                case "configuración":
                    MainFrame.Navigate(new Configuracion());
                    break;
                default:
                    MainFrame.Navigate(new Inicio());
                    break;
            }
        }

        #region Event Handlers
        private void Inicio_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnInicio);
            NavigateToPage("Inicio");
        }

        private void Clientes_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnClientes);
            NavigateToPage("Clientes");
        }

        private void Vehiculos_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnVehiculos);
            NavigateToPage("Vehículos");
        }

        private void Servicios_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnServicios);
            NavigateToPage("Trabajos");
        }

        private void Reservas_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnReservas);
            NavigateToPage("Reservas");
        }

        private void Inventario_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnInventario);
            NavigateToPage("Inventario");
        }

        private void Facturacion_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnFacturacion);
            NavigateToPage("Facturación");
        }

        private void Reportes_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnReportes);
            NavigateToPage("Reportes");
        }

        private void Configuracion_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnConfiguracion);
            NavigateToPage("Configuración");
        }
        #endregion
    }
}