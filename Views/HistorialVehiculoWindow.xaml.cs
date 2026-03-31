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
    public partial class HistorialVehiculoWindow : Window
    {
        private readonly int _vehiculoId;

        public HistorialVehiculoWindow(int vehiculoId)
        {
            InitializeComponent();
            _vehiculoId = vehiculoId;
            Loaded += (_, __) => Cargar();
        }

        private void Cargar()
        {
            try
            {
                using var db = new TallerDbContext();

                var vehiculo = db.Vehiculos
                    .Include(v => v.Cliente)
                    .FirstOrDefault(v => v.VehiculoID == _vehiculoId);

                if (vehiculo == null) { Close(); return; }

                // ── Header ────────────────────────────────────────────────────
                txtTitulo.Text = $"🚗  {vehiculo.Marca} {vehiculo.Modelo}";
                txtPlaca.Text = vehiculo.Placa;
                txtSubtitulo.Text =
                    $"Propietario: {vehiculo.Cliente?.Nombre} {vehiculo.Cliente?.Apellido}" +
                    (vehiculo.Anio.HasValue ? $"   ·   Año {vehiculo.Anio}" : "");

                // ── Trabajos ──────────────────────────────────────────────────
                var trabajos = db.Trabajos
                    .Where(t => t.VehiculoID == _vehiculoId)
                    .OrderByDescending(t => t.FechaIngreso)
                    .ToList();

                dgHistorial.ItemsSource = trabajos;

                // ── Estadísticas ──────────────────────────────────────────────
                txtTotalTrabajos.Text = trabajos.Count.ToString();

                decimal totalFacturado = db.Facturas
                    .Where(f => f.Estado == "Pagada"
                             && trabajos.Select(t => t.TrabajoID)
                                        .Contains(f.TrabajoID))
                    .Sum(f => (decimal?)f.Total) ?? 0;

                txtTotalFacturado.Text = $"Bs. {totalFacturado:N2}";

                var ultimo = trabajos.FirstOrDefault();
                txtUltimoIngreso.Text = ultimo != null
                    ? ultimo.FechaIngreso.ToString("dd/MM/yyyy")
                    : "—";

                int enCurso = trabajos.Count(t =>
                    t.Estado == "Pendiente" || t.Estado == "En Progreso");

                txtEnCurso.Text = enCurso.ToString();
                txtEnCurso.Foreground = enCurso > 0
                    ? (System.Windows.Media.Brush)FindResource("ColorWarning")
                    : (System.Windows.Media.Brush)FindResource("ColorSuccess");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void DgHistorial_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgHistorial.SelectedItem is not Trabajo trabajo) return;
            var win = new DetallesTrabajoWindow(trabajo.TrabajoID);
            win.ShowDialog();
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
