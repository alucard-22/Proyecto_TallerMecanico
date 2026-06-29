using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
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

namespace Proyecto_taller
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;

        public MainWindow()
        {
            InitializeComponent();

            ActualizarInfoUsuario();

            SetActiveButton(BtnInicio);
            NavigateToPage("Inicio");

            ActualizarIconoMaximizar();
            StateChanged += (s, e) => ActualizarIconoMaximizar();
        }

        // ── Barra de título personalizada ───────────────────────────────────

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            if (e.ClickCount == 2)
            {
                MaximizarRestaurar_Click(sender, e);
                return;
            }

            DragMove();
        }

        private void Minimizar_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizarRestaurar_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ActualizarIconoMaximizar()
        {
            if (btnMaximizar == null) return;
            btnMaximizar.Content = WindowState == WindowState.Maximized ? "❐" : "☐";
        }

        // ── Visibilidad de módulos según rol y permisos ──────────────────────

        private void ActualizarInfoUsuario()
        {
            if (!SessionManager.EstaAutenticado) return;

            bool esAdmin = SessionManager.EsAdministrador;

            // Mostrar nombre y rol del usuario logueado debajo del icono de perfil
            UsuarioActualTextBlock.Text = SessionManager.ObtenerNombreUsuario();
            UsuarioActualTextBlock.ToolTip =
                $"{SessionManager.ObtenerNombreUsuario()} ({SessionManager.ObtenerRol()})";

            BtnClientes.Visibility = SessionManager.TienePermiso("Clientes") ? Visibility.Visible : Visibility.Collapsed;
            BtnVehiculos.Visibility = SessionManager.TienePermiso("Vehiculos") ? Visibility.Visible : Visibility.Collapsed;
            BtnServicios.Visibility = SessionManager.TienePermiso("Trabajos") ? Visibility.Visible : Visibility.Collapsed;
            BtnReservas.Visibility = SessionManager.TienePermiso("Reservas") ? Visibility.Visible : Visibility.Collapsed;
            BtnInventario.Visibility = SessionManager.TienePermiso("Inventario") ? Visibility.Visible : Visibility.Collapsed;
            BtnFacturacion.Visibility = SessionManager.TienePermiso("Recibos") ? Visibility.Visible : Visibility.Collapsed;
            BtnReportes.Visibility = SessionManager.TienePermiso("Reportes") ? Visibility.Visible : Visibility.Collapsed;

            // Usuarios, Configuración y Auditoría: exclusivos de Administrador.
            BtnUsuarios.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnConfiguracion.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnAuditoria.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
            {
                _activeButton.Style = (Style)FindResource("MenuButton");
            }

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
                case "recibos":
                    MainFrame.Navigate(new Facturacion());
                    break;
                case "reportes":
                    MainFrame.Navigate(new Reportes());
                    break;
                case "configuración":
                    MainFrame.Navigate(new Configuracion());
                    break;
                case "usuarios":
                    MainFrame.Navigate(new Usuarios());
                    break;
                case "auditoría":
                    MainFrame.Navigate(new Auditoria());
                    break;
                default:
                    MainFrame.Navigate(new Inicio());
                    break;
            }
        }

        private bool VerificarYNavegar(string modulo, Button boton, string pageName)
        {
            if (!SessionManager.TienePermiso(modulo))
            {
                MessageBox.Show(
                    $"No tienes permiso para acceder al módulo '{modulo}'.\n\n" +
                    $"Contacta al administrador para solicitar acceso.",
                    "Acceso Denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            SetActiveButton(boton);
            NavigateToPage(pageName);
            return true;
        }

        private void Clientes_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Clientes", BtnClientes, "Clientes");

        private void Vehiculos_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Vehiculos", BtnVehiculos, "Vehículos");

        private void Servicios_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Trabajos", BtnServicios, "Trabajos");

        private void Reservas_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Reservas", BtnReservas, "Reservas");

        private void Inventario_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Inventario", BtnInventario, "Inventario");

        private void Facturacion_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Recibos", BtnFacturacion, "Recibos");

        private void Reportes_Click(object sender, RoutedEventArgs e)
            => VerificarYNavegar("Reportes", BtnReportes, "Reportes");

        private void Inicio_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnInicio);
            NavigateToPage("Inicio");
        }

        private void Configuracion_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EsAdministrador)
            {
                MessageBox.Show(
                    "Solo los administradores pueden acceder a la configuración del sistema.\n\n" +
                    "Contacta al administrador si necesitas realizar algún cambio aquí.",
                    "Acceso Denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SetActiveButton(BtnConfiguracion);
            NavigateToPage("Configuración");
        }

        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EsAdministrador)
            {
                MessageBox.Show(
                    "No tienes permisos para acceder a esta sección.",
                    "Acceso Denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SetActiveButton(BtnUsuarios);
            NavigateToPage("Usuarios");
        }

        // ── NUEVO: navegación a Auditoría, exclusiva de Administrador ────────
        private void Auditoria_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.EsAdministrador)
            {
                MessageBox.Show(
                    "Solo los administradores pueden ver el registro de auditoría.",
                    "Acceso Denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SetActiveButton(BtnAuditoria);
            NavigateToPage("Auditoría");
        }

        private void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Está seguro de cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                SessionManager.CerrarSesion();

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                this.Close();
            }
        }
    }
}