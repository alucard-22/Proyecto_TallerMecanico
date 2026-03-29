using Proyecto_taller.Data;
using Proyecto_taller.Models;
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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class HistorialClienteWindow : Window
    {
        private readonly int _clienteId;

        public HistorialClienteWindow(int clienteId)
        {
            InitializeComponent();
            _clienteId = clienteId;

            // Ajustar al área de trabajo disponible para no salir de pantalla
            var workArea = SystemParameters.WorkArea;
            Width = Math.Min(Width, workArea.Width * 0.92);
            Height = Math.Min(Height, workArea.Height * 0.90);

            Loaded += (_, __) => Cargar();
        }

        // ─────────────────────────────────────────────────────────
        //  CARGA DE DATOS
        // ─────────────────────────────────────────────────────────

        private void Cargar()
        {
            try
            {
                using var db = new TallerDbContext();

                var cliente = db.Clientes
                    .Include(c => c.Vehiculos)
                    .FirstOrDefault(c => c.ClienteID == _clienteId);

                if (cliente == null) { Close(); return; }

                // ── Header ────────────────────────────────────────
                txtNombreCliente.Text = $"{cliente.Nombre} {cliente.Apellido}";
                txtInfoCliente.Text =
                    $"📞 {cliente.Telefono}" +
                    (string.IsNullOrWhiteSpace(cliente.Correo) ? "" : $"   ·   ✉️ {cliente.Correo}") +
                    $"   ·   Registrado el {cliente.FechaRegistro:dd/MM/yyyy}";

                // ── Vehículos (con conteo de trabajos eager-loaded) ─
                var vehiculos = db.Vehiculos
                    .Include(v => v.Trabajos)
                    .Where(v => v.ClienteID == _clienteId)
                    .OrderBy(v => v.Placa)
                    .ToList();

                dgVehiculos.ItemsSource = vehiculos;
                txtTotalVehiculos.Text = vehiculos.Count.ToString();

                // ── Trabajos de todos los vehículos del cliente ────
                var vehiculoIds = vehiculos.Select(v => v.VehiculoID).ToList();

                var trabajos = db.Trabajos
                    .Include(t => t.Vehiculo)
                    .Where(t => vehiculoIds.Contains(t.VehiculoID))
                    .OrderByDescending(t => t.FechaIngreso)
                    .ToList();

                dgTrabajos.ItemsSource = trabajos;
                txtTotalTrabajos.Text = trabajos.Count.ToString();

                // ── Estadísticas ──────────────────────────────────
                decimal totalFacturado = db.Facturas
                    .Where(f => f.Estado == "Pagada"
                             && trabajos.Select(t => t.TrabajoID).Contains(f.TrabajoID))
                    .Sum(f => (decimal?)f.Total) ?? 0;

                txtTotalFacturado.Text = $"Bs. {totalFacturado:N2}";

                int activos = trabajos.Count(t =>
                    t.Estado == "Pendiente" || t.Estado == "En Progreso");

                txtTrabajosActivos.Text = activos.ToString();
                txtTrabajosActivos.Foreground = activos > 0
                    ? (Brush)FindResource("ColorWarning")
                    : (Brush)FindResource("ColorSuccess");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial del cliente:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  TABS
        // ─────────────────────────────────────────────────────────

        private void TabVehiculos_Click(object sender, RoutedEventArgs e)
        {
            panelVehiculos.Visibility = Visibility.Visible;
            panelTrabajos.Visibility = Visibility.Collapsed;

            btnTabVehiculos.Style = (Style)FindResource("BtnPrimary");
            btnTabTrabajos.Style = (Style)FindResource("BtnSecondary");

            txtTipDoble.Text = "Doble clic para ver el historial de trabajos del vehículo";
        }

        private void TabTrabajos_Click(object sender, RoutedEventArgs e)
        {
            panelVehiculos.Visibility = Visibility.Collapsed;
            panelTrabajos.Visibility = Visibility.Visible;

            btnTabVehiculos.Style = (Style)FindResource("BtnSecondary");
            btnTabTrabajos.Style = (Style)FindResource("BtnPrimary");

            txtTipDoble.Text = "Doble clic para ver los detalles del trabajo";
        }

        // ─────────────────────────────────────────────────────────
        //  DOBLE CLIC EN GRIDS
        // ─────────────────────────────────────────────────────────

        private void DgVehiculos_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgVehiculos.SelectedItem is not Vehiculo vehiculo) return;
            var win = new HistorialVehiculoWindow(vehiculo.VehiculoID);
            win.ShowDialog();
        }

        private void DgTrabajos_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgTrabajos.SelectedItem is not Trabajo trabajo) return;
            var win = new DetallesTrabajoWindow(trabajo.TrabajoID);
            win.ShowDialog();
        }

        // ─────────────────────────────────────────────────────────
        //  CERRAR
        // ─────────────────────────────────────────────────────────

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}

